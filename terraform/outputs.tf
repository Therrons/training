output "github_actions_role_arn" {
  description = "ARN of the IAM role assumed by GitHub Actions via OIDC"
  value       = aws_iam_role.github_actions.arn
}

output "secret_arn" {
  description = "ARN of the PostgreSQL credentials secret"
  value       = aws_secretsmanager_secret.postgres.arn
}

output "secret_name" {
  description = "Name of the Secrets Manager secret (matches AWSSecretName config key)"
  value       = aws_secretsmanager_secret.postgres.name
}

output "aws_region" {
  description = "AWS region resources were deployed into"
  value       = var.aws_region
}
