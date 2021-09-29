const df = require("durable-functions");
const { v4: uuidv4 } = require('uuid');
const moment = require('moment');

module.exports = df.orchestrator(function* (context) {
    
    const outputs = {
        totalSuccessDocument: 0,
        totalErrorDocument: 0,
        totalDocumentProcess: 0,
        startedTime: moment.utc(new Date()).format(),
        endedTime: 0
    };

    const payload = context.df.getInput();
    const containerName = payload.containerName;
    let factor = process.env.FACTOR || 1000;

    let left = 0;
    let index = 0;

    if (payload.nbrDocuments > factor) {
        left = payload.nbrDocuments % factor;
        index = Math.floor(payload.nbrDocuments / factor);        
    } else {
        index = 1;
        factor = payload.nbrDocuments;
    }

    context.log.info(`Left: ${left}`);
    context.log.info(`Index: ${index}`);
    context.log.info(`Factor: ${factor}`);
    context.log.info(`Nbr of documents: ${payload.nbrDocuments}`);

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
    
    outputs.totalDocumentProcess = payload.nbrDocuments;

    outputs.endedTime = moment.utc(new Date()).format();

    return outputs;
});

function aggregate(outputs,results) {
    const documentsInError = results.filter(r => r.isSuccess === false);
    const successDocument = results.filter(r => r.isSuccess).length;
    
    outputs.totalSuccessDocument += successDocument;
    outputs.totalErrorDocument += documentsInError.length;        
}

function startActivities(factor,context,containerName){
    const tasks = [];
    for (let y = 0; y < factor; y++) {
        const filename = `${uuidv4()}.pdf`;      
        const input = {
            containerName: containerName,
            filename: filename
        };
        tasks.push(context.df.callActivity('UploadInvoices',input));           
    }     
    return tasks;
}




