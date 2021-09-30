param location string

var suffix = uniqueString(resourceGroup().id)

module cognitives 'modules/cognitives/form.bicep' = {
  name: 'cognitives'
  params: {
    location: location
    suffix: suffix
  }
}

module storage 'modules/storage/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    suffix: suffix
  }
}
