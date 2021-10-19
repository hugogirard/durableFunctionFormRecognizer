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
MaxRetries | Maximum number of retries | 3
RetryMillisecondsPower | The power used for exponential backoff in ms (ex: 2 => 2^1=2, 2^2=4, 2^3=8) | 2
RetryMillisecondsFactor | The factor used for exponential backoff in ms (ex: 1000 => 2^1x1000=2000) | 1000
FormRecognizerEndpoint | Form Recognizer endpoint | N/A
FormRecognizerKey | Form Recognizer authentication key | N/A
FormRecognizerModelId | Form Recognizer model Id | N/A
FormRecognizerTPS | The number of TPS of the Form Recognizer instance | 15
FormRecognizerMinWaitTime | Minimum wait time after submitting a document to Form Recognizer before querying for result | 10 seconds
TableStorageConnectionString | Connection string for the target table storage | N/A
TableStorageTableName | Name of target table in table storage | N/A
<!-- CosmosEndpoint | Target Cosmos endpoint | N/A
CosmosAuthKey | Target Cosmos authentication key | N/A
CosmosDatabaseId | Target Cosmos database | N/A
CosmosContainerId | Target Cosmos container | N/A -->