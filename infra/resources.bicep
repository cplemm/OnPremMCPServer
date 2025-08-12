@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}

param mcpserverExists bool
param webclientExists bool

// @description('Id of the user or app to assign application roles')
// param principalId string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = uniqueString(subscription().id, resourceGroup().id, location)

// Monitor application with Azure Monitor
module monitoring 'br/public:avm/ptn/azd/monitoring:0.1.0' = {
  name: 'monitoring'
  params: {
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: '${abbrs.portalDashboards}${resourceToken}'
    location: location
    tags: tags
  }
}

// Azure Relay
module relayNamespace 'br/public:avm/res/relay/namespace:0.7.2' = {
  name: 'relayDeployment'
  params: {
    name: 'relay-${resourceToken}'
    location: location
    tags: tags
    skuName: 'Standard'
    hybridConnections: [
      {
        name: 'mcp-hc'
        userMetadata: '[{\'key\':\'endpoint\',\'value\':\'\'}]'
      }
    ]
  }
}

resource relay 'Microsoft.Relay/namespaces@2024-01-01' existing = {
  name: 'relay-${resourceToken}'
  dependsOn: [
    relayNamespace
  ]
}

resource rootRule 'Microsoft.Relay/namespaces/authorizationRules@2024-01-01' existing = {
  name: 'RootManageSharedAccessKey'
  parent: relay
}

var root = rootRule.listKeys()
var relayPrimaryKey = root.primaryKey

// Container registry
module containerRegistry 'br/public:avm/res/container-registry/registry:0.1.1' = {
  name: 'registry'
  params: {
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: tags
    publicNetworkAccess: 'Enabled'
    roleAssignments:[
      {
        principalId: mcpserverIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
      }
      {
        principalId: webclientIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
      }
    ]
  }
}

module csAccount 'br/public:avm/res/cognitive-services/account:0.12.0' = {
  name: 'csAccount'
  params: {
    kind: 'AIServices'
    location: location
    tags: tags
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    deployments: [
      {
        model: {
          format: 'OpenAI'
          name: 'gpt-4o-mini'
          version: '2024-07-18'
        }
        name: 'gpt-4o-mini'
        sku: {
          capacity: 50
          name: 'GlobalStandard'
        }
      }
    ]
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// Container apps environment
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.4.5' = {
  name: 'container-apps-environment'
  params: {
    logAnalyticsWorkspaceResourceId: monitoring.outputs.logAnalyticsWorkspaceResourceId
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    zoneRedundant: false
  }
}

module mcpserverIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.2.1' = {
  name: 'mcpserveridentity'
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}mcpserver-${resourceToken}'
    location: location
  }
}

module mcpserverFetchLatestImage './modules/fetch-container-image.bicep' = {
  name: 'mcpserver-fetch-image'
  params: {
    exists: mcpserverExists
    name: 'mcpserver'
  }
}

module mcpserver 'br/public:avm/res/app/container-app:0.8.0' = {
  name: 'mcpserver'
  params: {
    name: 'mcpserver'
    ingressTargetPort: 8080
    ingressExternal: false
    scaleMinReplicas: 1
    scaleMaxReplicas: 10
    secrets: {
      secureList:  [
      ]
    }
    containers: [
      {
        image: mcpserverFetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        name: 'main'
        resources: {
          cpu: json('0.5')
          memory: '1.0Gi'
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: monitoring.outputs.applicationInsightsConnectionString
          }
          {
            name: 'AZURE_CLIENT_ID'
            value: mcpserverIdentity.outputs.clientId
          }
          {
            name: 'PORT'
            value: '8080'
          }
          {
            name: 'RelayConfiguration__Namespace'
            value: '${relayNamespace.outputs.name}.servicebus.windows.net'
          }
          {
            name: 'RelayConfiguration__HybridConnectionPath'
            value: 'mcp-hc'
          }
          {
            name: 'RelayConfiguration__SasKeyName'
            value: 'RootManageSharedAccessKey'
          }
          {
            name: 'RelayConfiguration__SasKey'
            value: relayPrimaryKey
          }
        ]
      }
    ]
    managedIdentities:{
      systemAssigned: false
      userAssignedResourceIds: [mcpserverIdentity.outputs.resourceId]
    }
    registries:[
      {
        server: containerRegistry.outputs.loginServer
        identity: mcpserverIdentity.outputs.resourceId
      }
    ]
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, { 'azd-service-name': 'mcpserver' })
  }
}

module webclientIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.2.1' = {
  name: 'webclientidentity'
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}webclient-${resourceToken}'
    location: location
  }
}

module webclientFetchLatestImage './modules/fetch-container-image.bicep' = {
  name: 'webclient-fetch-image'
  params: {
    exists: webclientExists
    name: 'webclient'
  }
}

resource openai 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
  dependsOn: [
    csAccount
  ]
}

module webclient 'br/public:avm/res/app/container-app:0.8.0' = {
  name: 'webclient'
  dependsOn: [
    csAccount
  ]
  params: {
    name: 'webclient'
    ingressTargetPort: 8080
    scaleMinReplicas: 1
    scaleMaxReplicas: 10
    secrets: {
      secureList:  [
      ]
    }
    containers: [
      {
        image: webclientFetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        name: 'main'
        resources: {
          cpu: json('0.5')
          memory: '1.0Gi'
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: monitoring.outputs.applicationInsightsConnectionString
          }
          {
            name: 'AZURE_CLIENT_ID'
            value: webclientIdentity.outputs.clientId
          }
          {
            name: 'PORT'
            value: '8080'
          }
          {
            name: 'McpClient__ServerUrl'
            value: 'http://mcpserver/'
          }
          {
            name: 'AzureOpenAI__Endpoint'
            value: 'https://${location}.api.cognitive.microsoft.com/'
          }
          {
            name: 'AzureOpenAI__DeploymentName'
            value: 'gpt-4o-mini'
          }
          {
            name: 'AzureOpenAI__ApiKey'
            value: openai.listKeys().key1
          }
        ]
      }
    ]
    managedIdentities:{
      systemAssigned: false
      userAssignedResourceIds: [webclientIdentity.outputs.resourceId]
    }
    registries:[
      {
        server: containerRegistry.outputs.loginServer
        identity: webclientIdentity.outputs.resourceId
      }
    ]
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, { 'azd-service-name': 'webclient' })
  }
}
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_RESOURCE_MCPSERVER_ID string = mcpserver.outputs.resourceId
output AZURE_RESOURCE_WEBCLIENT_ID string = webclient.outputs.resourceId
