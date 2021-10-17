param location string
param suffix string

var appPlanName = 'plan-${suffix}'
var webAppName = 'blazor-admin-${suffix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2021-01-15' = {
  name: appPlanName
  location: location
  sku: {
    name: 'S1'
  }
  kind: 'linux'
}


resource appService 'Microsoft.Web/sites@2018-11-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|3.1'
      alwaysOn: true
    }
  }
}


output appName string = webAppName
