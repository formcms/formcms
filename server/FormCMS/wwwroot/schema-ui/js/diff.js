import {loadNavBar} from "./nav-bar.js";
import {save, single} from "./repo.js";
$(document).ready(function() {
    const searchParams = new URLSearchParams(window.location.search);
    const id1 = searchParams.get("id1");
    const id2 = searchParams.get("id2");

    loadNavBar("#nav-box");
    initializeEditor("#diff-box",id1,id2);
});

function initializeEditor(container, id1,id2) {
    require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' } });
    require(["vs/editor/editor.main"], async function () {
        const diffEditor = monaco.editor.createDiffEditor($(container)[0], {
            theme: "vs-dark",
            readOnly: false,
            enableSplitViewResizing: true,
        });
        
        $.LoadingOverlay("show");
        let [{data: schema1, error: err1},{data: schema2, error:err2}] = await Promise.all([ single(id1), single(id2)]);
        $.LoadingOverlay("hide");
        
        if (err1|| err2){
            const error = err1.error + err2.error;
            $('#errorPanel').text(error).show(); 
            return;
        }
        const type = schema1.type;

        schema1 = schema1.settings[type];
        schema2 = schema2.settings[type];
        
        let lan; 
        if(type === 'query') {
            lan = "graphql";
            schema1 = schema1.source;
            schema2 = schema2.source;
        }else if (type === 'page') {
            lan = "json";
            schema1 = JSON.stringify(JSON.parse(schema1.components),null, 2);
            schema2 = JSON.stringify(JSON.parse(schema2.components),null, 2);
        }else{
            lan = "json";
            schema1 = JSON.stringify(schema1,null,2);
            schema2 = JSON.stringify(schema2,null,2);
        }
        
        
        diffEditor.setModel({
            original: monaco.editor.createModel(schema1, lan),
            modified: monaco.editor.createModel(schema2, lan),
        });
    });
}