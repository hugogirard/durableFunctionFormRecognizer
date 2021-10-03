const df = require("durable-functions");
const appInsights = require('applicationinsights');
appInsights.setup()
appInsights.defaultClient.context.tags[appInsights.defaultClient.context.keys.cloudRole] = "SeederFunction";
appInsights.defaultClient.setAutoPopulateAzureProperties(true);
appInsights.start();

module.exports = async function (context, req) {

    try {
        // Get payload HTTP Post
        if (req.query.nbrDocument) {
            const client = df.getClient(context);
            const instanceId = await client.startNew('SeederOrchestrator', undefined, req.query.nbrDocument);
        
            context.log(`Started orchestration with ID = '${instanceId}'.`);
        
            return client.createCheckStatusResponse(context.bindingData.req, instanceId);        
        } else {
            context.log.error('No documents passed in query parameters');
            throw new Error('Please pass the nbrDocument in the query string');
        }        

    } catch (error) {
        context.log.error(error);
        throw new Error(error);
    }


};