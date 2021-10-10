# Settings

Name | Description | Default value
---- | ----------- | -------------
BatchSize | Number of blobs to collect in one pass | 2
MinBacklogSize | Number of blobs the collector keeps ready to process | 4
NbPartitions | Number of partitions for processing | 1
BlobContainerName | Name of source the blob container | N/A
StorageAccountConnectionString | Connection string of the source storage | N/A
CollectDelay | Delay between collection passes | 10 seconds
NoDataDelay | Delay between processing passes when no data | 10 seconds
MinProcessingTime | Minimum wait after submitting a document to Form Recognizer | 10 seconds
MaxRetries | Maximum number of retries | 3
FormRecognizerEndpoint | Form Recognizer endpoint | N/A
FormRecognizerKey | Form Recognizer authentication key | N/A
FormRecognizerModelId | Form Recognizer model Id | N/A
CosmosEndpoint | Target Cosmos endpoint | N/A
CosmosAuthKey | Target Cosmos authentication key | N/A
CosmosDatabaseId | Target Cosmos database | N/A
CosmosContainerId | Target Cosmos container | N/A