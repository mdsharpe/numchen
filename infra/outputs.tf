output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "container_app_name" {
  value = azurerm_container_app.api.name
}

output "api_fqdn" {
  value = azurerm_container_app.api.ingress[0].fqdn
}

output "static_web_app_name" {
  value = azurerm_static_web_app.ui.name
}

output "static_web_app_hostname" {
  value = azurerm_static_web_app.ui.default_host_name
}

output "deploy_client_id" {
  value = azurerm_user_assigned_identity.deploy.client_id
}

output "tenant_id" {
  value = data.azurerm_client_config.current.tenant_id
}

output "subscription_id" {
  value = data.azurerm_client_config.current.subscription_id
}
