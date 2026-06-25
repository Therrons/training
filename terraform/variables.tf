variable "aws_region" {
  description = "AWS region to deploy resources into"
  type        = string
  default     = "af-south-1"
}

variable "project_name" {
  description = "Short name used as a prefix for all resources"
  type        = string
  default     = "fraud_poc_project"
}

variable "environment" {
  description = "Deployment environment (e.g. dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "github_org" {
  description = "GitHub organisation or user that owns the repository"
  type        = string
}

variable "github_repo" {
  description = "GitHub repository name (without the org prefix)"
  type        = string
}

variable "create_oidc_provider" {
  description = "Set to false if the GitHub OIDC provider already exists in this AWS account"
  type        = bool
  default     = true
}

variable "secret_name" {
  description = "Name of the Secrets Manager secret – must match AWSSecretName in appsettings"
  type        = string
  default     = "credit-plrcre-npr/fraud_poc_project_K8s/dev"
}

variable "db_username" {
  description = "PostgreSQL username to store in Secrets Manager"
  type        = string
  default     = "docke_user"
}

variable "db_password" {
  description = "PostgreSQL password to store in Secrets Manager (supply via TF_VAR_db_password in CI)"
  type        = string
  sensitive   = true
}
