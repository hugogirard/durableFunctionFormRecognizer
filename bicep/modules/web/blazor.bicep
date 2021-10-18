param location string
param suffix string

param appInsightKey string
param appInsightCnxString string

var appPlanName = 'plan-blazor-${suffix}'
var webAppName = 'blazor-admin-${suffix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2021-01-15' = {
  name: appPlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}


resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    clientAffinityEnabled: false
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightCnxString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
      ]
      metadata: [
        {
           name: 'CURRENT_STACK'
           value: 'dotnetcore'
        }
      ]
      alwaysOn: true
    }
  }
}


output appName string = webAppName
