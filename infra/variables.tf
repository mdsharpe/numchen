variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "project" {
  description = "Project name, used as a prefix for all resources"
  type        = string
  default     = "numchen"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "uksouth"
}

variable "github_repo" {
  description = "GitHub repository in owner/repo format (e.g. mdsharpe/numchen)"
  type        = string
}
