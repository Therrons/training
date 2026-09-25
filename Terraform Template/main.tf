terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = "af-south-1"
}

# ══════════════════════════════════════════════════════════════════════════════
# VARIABLES - Fraud POC Secrets
# ══════════════════════════════════════════════════════════════════════════════

variable "db_username" {
  description = "Database username for fraud POC project"
  type        = string
  sensitive   = true
}

variable "db_password" {
  description = "Database password for fraud POC project"
  type        = string
  sensitive   = true
}

variable "kafka_user" {
  description = "Kafka username for fraud POC project"
  type        = string
  sensitive   = true
}

variable "kafka_password" {
  description = "Kafka admin password for fraud POC project"
  type        = string
  sensitive   = true
}

# ══════════════════════════════════════════════════════════════════════════════
# AWS SECRETS MANAGER - Single Secret with Multiple Key-Value Pairs
# ══════════════════════════════════════════════════════════════════════════════

resource "aws_secretsmanager_secret" "fraud_poc_secrets" {
  name                    = "fraud_poc_secrets"
  description             = "Consolidated credentials for fraud POC project (database and Kafka)"
  recovery_window_in_days = 7

  tags = {
    Environment = "dev"
    Project     = "fraud-poc-project"
    ManagedBy   = "terraform"
    Type        = "credentials"
  }

  # ✅ Prevent accidental deletion
  lifecycle {
    prevent_destroy = true
  }
}

resource "aws_secretsmanager_secret_version" "fraud_poc_secrets_value" {
  secret_id = aws_secretsmanager_secret.fraud_poc_secrets.id
  secret_string = jsonencode({
    db_username    = var.db_username
    db_password    = var.db_password
    kafka_user     = var.kafka_user
    kafka_password = var.kafka_password
  })

  # ✅ Uncomment below to prevent updates to existing secrets
  # This keeps existing values and only updates new versions when values change
  lifecycle {
    ignore_changes = [secret_string]
  }
}

# ══════════════════════════════════════════════════════════════════════════════
# IAM ROLE: fraud-poc-aws-role
# ══════════════════════════════════════════════════════════════════════════════

resource "aws_iam_role" "fraud_POC_role" {
  name               = "fraud-poc-aws-role"
  description        = "GitHub Actions role for fraud POC project with Terraform execution capabilities"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Federated = "arn:aws:iam::060440565535:oidc-provider/token.actions.githubusercontent.com"
        }
        Action = "sts:AssumeRoleWithWebIdentity"
        Condition = {
          StringEquals = {
            "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
          }
          StringLike = {
            "token.actions.githubusercontent.com:sub" = [
              "repo:Therrons/training:ref:refs/heads/K8S",
              "repo:Therrons/training:ref:refs/heads/K8S"
            ]
          }
        }
      }
    ]
  })

  tags = {
    Environment = "dev"
    ManagedBy   = "terraform"
    Project     = "fraud-poc-project"
  }

  depends_on = [
    aws_secretsmanager_secret.fraud_poc_secrets
  ]
}

# ══════════════════════════════════════════════════════════════════════════════
# INLINE POLICY 1: ReadFraudPocSecrets
# Allows reading the fraud_poc_secrets from AWS Secrets Manager
# ══════════════════════════════════════════════════════════════════════════════

resource "aws_iam_role_policy" "read_fraud_poc_secrets" {
  name   = "ReadFraudPocSecrets"
  role   = aws_iam_role.fraud_POC_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowReadFraudPOCSecrets"
        Effect = "Allow"
        Action = [
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret"
        ]
        Resource = [
          aws_secretsmanager_secret.fraud_poc_secrets.arn
        ]
      }
    ]
  })
}

# ══════════════════════════════════════════════════════════════════════════════
# INLINE POLICY 2: TerraformExecution
# Allows running Terraform with permissions to manage secrets and IAM roles
# ══════════════════════════════════════════════════════════════════════════════

resource "aws_iam_role_policy" "terraform_execution" {
  name   = "TerraformExecution"
  role   = aws_iam_role.fraud_POC_role.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "ManageSecrets"
        Effect = "Allow"
        Action = [
          "secretsmanager:CreateSecret",
          "secretsmanager:UpdateSecret",
          "secretsmanager:DeleteSecret",
          "secretsmanager:GetSecretValue",
          "secretsmanager:DescribeSecret",
          "secretsmanager:ListSecrets",
          "secretsmanager:TagResource",
          "secretsmanager:UntagResource"
        ]
        Resource = "*"
      },
      {
        Sid    = "ManageIAMRoles"
        Effect = "Allow"
        Action = [
          "iam:CreateRole",
          "iam:GetRole",
          "iam:UpdateAssumeRolePolicy",
          "iam:DeleteRole",
          "iam:PutRolePolicy",
          "iam:DeleteRolePolicy",
          "iam:GetRolePolicy",
          "iam:ListRolePolicies",
          "iam:AttachRolePolicy",
          "iam:DetachRolePolicy",
          "iam:ListAttachedRolePolicies",
          "iam:ListRoles",
          "iam:TagRole",
          "iam:UntagRole"
        ]
        Resource = "*"
      },
      {
        Sid    = "ListOIDCProviders"
        Effect = "Allow"
        Action = [
          "iam:ListOpenIDConnectProviders"
        ]
        Resource = "*"
      }
    ]
  })
}

# ══════════════════════════════════════════════════════════════════════════════
# OUTPUTS - IAM Role
# ══════════════════════════════════════════════════════════════════════════════

output "role_arn" {
  description = "ARN of the fraud-poc-aws-role"
  value       = aws_iam_role.fraud_POC_role.arn
}

output "role_name" {
  description = "Name of the fraud-poc-aws-role"
  value       = aws_iam_role.fraud_POC_role.name
}

output "role_id" {
  description = "ID of the fraud-poc-aws-role"
  value       = aws_iam_role.fraud_POC_role.id
}

# ══════════════════════════════════════════════════════════════════════════════
# OUTPUTS - Secrets
# ══════════════════════════════════════════════════════════════════════════════

output "fraud_poc_secrets_arn" {
  description = "ARN of the fraud_poc_secrets secret"
  value       = aws_secretsmanager_secret.fraud_poc_secrets.arn
  sensitive   = true
}

output "fraud_poc_secrets_name" {
  description = "Name of the fraud_poc_secrets secret"
  value       = aws_secretsmanager_secret.fraud_poc_secrets.name
}

output "fraud_poc_secrets_keys" {
  description = "Keys contained in the fraud_poc_secrets secret"
  value = {
    db_username    = "username value"
    db_password    = "password value"
    kafka_user     = "kafka user value"
    kafka_password = "kafka password value"
  }
  sensitive = true
}

output "all_resources_summary" {
  description = "Summary of all created resources"
  value = {
    iam_role             = aws_iam_role.fraud_POC_role.name
    inline_policies      = ["ReadFraudPocSecrets", "TerraformExecution"]
    secret_name          = aws_secretsmanager_secret.fraud_poc_secrets.name
    secret_keys          = ["db_username", "db_password", "kafka_user", "kafka_password"]
    total_resources      = 3
    region               = "af-south-1"
  }
}