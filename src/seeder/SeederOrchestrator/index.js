const df = require("durable-functions");
const { v4: uuidv4 } = require('uuid');
const moment = require('moment');

module.exports = df.orchestrator(function* (context) {
    
    let outputs = {};

    if (!context.is_replaying) {
        outputs = {
            totalSuccessDocument: 0,
            totalErrorDocument: 0,
            totalDocumentProcess: 0,
            startedTime: moment.utc(context.df.currentUtcDateTime).format(),
            endedTime: 0
        };
    }


    const nbrDocuments = context.df.getInput();
    const containerName = 'documents';

    // Send 1000 activities to write to storage at the time 
    // or you can increase the throughput changing the
    // environment variable value
    let factor = process.env.FACTOR || 1000;

    let left = 0;
    let index = 0;

    // Calculate how many loop will need to be done
    if (nbrDocuments > factor) {
        left = nbrDocuments % factor;
        index = Math.floor(nbrDocuments / factor);        
    } else {
        index = 1;
        factor = nbrDocuments;
    }

    context.log.info(`Left: ${left}`);
    context.log.info(`Index: ${index}`);
    context.log.info(`Factor: ${factor}`);
    context.log.info(`Nbr of documents: ${nbrDocuments}`);

    for (let i = 0; i < index; i++) {

        const tasks = startActivities(factor,context,containerName);
        const results = yield context.df.Task.all(tasks);
        aggregate(outputs,results);
    
    }

    if (left > 0){
        const tasks = startActivities(left,context,containerName);
        const results = yield context.df.Task.all(tasks);
        aggregate(outputs,results);    
    }
    
    outputs.totalDocumentProcess = nbrDocuments;

    //outputs.endedTime = moment.utc(new Date()).format();

    return outputs;
});

function aggregate(outputs,results) {
    const documentsInError = results.filter(r => r.isSuccess === false);
    const successDocument = results.filter(r => r.isSuccess).length;
    
    outputs.totalSuccessDocument += successDocument;
    outputs.totalErrorDocument += documentsInError.length;        
}

// Send X (factor) activities to write to storage
function startActivities(factor,context,containerName){
    const tasks = [];
    for (let y = 0; y < factor; y++) {
        const filename = `${context.df.newGuid()}-${moment.utc(context.df.currentUtcDateTime).format()}.pdf`;              
        const input = {
            containerName: containerName,
            filename: filename
        };
        tasks.push(context.df.callActivity('UploadInvoices',input));           
    }     
    return tasks;
}




