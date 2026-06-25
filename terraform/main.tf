terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  required_version = ">= 1.5.0"
}

provider "aws" {
  region = var.aws_region
}

# ── Data ──────────────────────────────────────────────────────────────────────

data "aws_caller_identity" "current" {}

# ── GitHub OIDC Identity Provider ─────────────────────────────────────────────
# Only one provider per URL is allowed per AWS account.
# Set create_oidc_provider = false if it already exists.

resource "aws_iam_openid_connect_provider" "github" {
  count = var.create_oidc_provider ? 1 : 0

  url            = "https://token.actions.githubusercontent.com"
  client_id_list = ["sts.amazonaws.com"]

  # GitHub's stable OIDC thumbprint – regenerate with:
  #   openssl s_client -connect token.actions.githubusercontent.com:443 2>/dev/null \
  #     | openssl x509 -fingerprint -noout -sha1
  thumbprint_list = ["6938fd4d98bab03faadb97b34396831e3780aea1"]
}

locals {
  oidc_provider_arn = var.create_oidc_provider ? (
    aws_iam_openid_connect_provider.github[0].arn
  ) : "arn:aws:iam::${data.aws_caller_identity.current.account_id}:oidc-provider/token.actions.githubusercontent.com"

  oidc_provider_url = "token.actions.githubusercontent.com"

  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

# ── IAM Role – assumed by GitHub Actions via OIDC ─────────────────────────────

data "aws_iam_policy_document" "github_actions_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [local.oidc_provider_arn]
    }

    # Audience must be sts.amazonaws.com when using aws-actions/configure-aws-credentials v4+
    condition {
      test     = "StringEquals"
      variable = "${local.oidc_provider_url}:aud"
      values   = ["sts.amazonaws.com"]
    }

    # Restrict to this specific repository – wildcard allows all branches/tags/envs
    condition {
      test     = "StringLike"
      variable = "${local.oidc_provider_url}:sub"
      values   = ["repo:${var.github_org}/${var.github_repo}:*"]
    }
  }
}

resource "aws_iam_role" "github_actions" {
  name               = "${var.project_name}-github-actions"
  assume_role_policy = data.aws_iam_policy_document.github_actions_assume_role.json
  tags               = local.common_tags
}

# ── Secrets Manager – PostgreSQL credentials ──────────────────────────────────
# Secret name matches the existing AWSSecretName used in appsettings.LOC.json
# and in the Dockerfile CMD: "credit-plrcre-npr/fraud_poc_project_K8s/dev"

resource "aws_secretsmanager_secret" "postgres" {
  name                    = var.secret_name
  description             = "PostgreSQL username and password for ${var.project_name} (${var.environment})"
  recovery_window_in_days = 0 # instant delete – fine for POC/dev

  tags = local.common_tags
}

resource "aws_secretsmanager_secret_version" "postgres" {
  secret_id = aws_secretsmanager_secret.postgres.id
  secret_string = jsonencode({
    username = var.db_username
    password = var.db_password
  })
}

# ── IAM Policy – GitHub Actions role can read only this secret ────────────────

data "aws_iam_policy_document" "read_postgres_secret" {
  statement {
    sid    = "ReadPostgresSecret"
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue",
      "secretsmanager:DescribeSecret",
    ]
    resources = [aws_secretsmanager_secret.postgres.arn]
  }
}

resource "aws_iam_role_policy" "github_actions_read_secret" {
  name   = "read-postgres-secret"
  role   = aws_iam_role.github_actions.id
  policy = data.aws_iam_policy_document.read_postgres_secret.json
}
