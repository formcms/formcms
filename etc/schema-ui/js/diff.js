import {loadNavBar} from "./components/navbar.js";
import {single} from "./services/services.js";
import {getParams} from "./util/searchParamUtil.js";
import {queryKeys} from "./models/types.js";
import {hideOverlay, showOverlay} from "./util/loadingOverlay.js";

const [navBox, headerBox, diffBox] = ["#nav-box", "#header-box", "#diff-box"];

const [oldId, newId, schemaId, type] = getParams([
    queryKeys.oldId,
    queryKeys.newId,
    queryKeys.schemaId,
    queryKeys.type
]);

addActionButtons(schemaId, type);
loadNavBar(navBox);
initializeEditor(oldId, newId);

function addActionButtons(schemaId, type) {
    const header = document.querySelector(headerBox);
    header.innerHTML = `
        <a href="history.html?${queryKeys.schemaId}=${schemaId}&${queryKeys.type}=${type}"  
           class="btn btn-secondary btn-sm badge">Back</a>
    `;
}

function initializeEditor(oldId, newId) {
    require.config({
        paths: {
            'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs'
        }
    });

    require(["vs/editor/editor.main"], async function () {
        const diffContainer = document.querySelector(diffBox);

        const diffEditor = monaco.editor.createDiffEditor(diffContainer, {
            theme: "vs-dark",
            readOnly: false,
            enableSplitViewResizing: true,
        });

        showOverlay();

        const [oldResult, newResult] = await Promise.all([
            single(oldId),
            single(newId)
        ]);

        hideOverlay();

        if (oldResult.error || newResult.error) {
            const errorPanel = document.getElementById('errorPanel');
            errorPanel.textContent = (oldResult.error || '') + (newResult.error || '');
            errorPanel.style.display = 'block';
            return;
        }

        let oldSchema = oldResult.data;
        let newSchema = newResult.data;
        const type = oldSchema.type;

        oldSchema = oldSchema.settings[type];
        newSchema = newSchema.settings[type];

        let lan;
        let originalCode, modifiedCode;

        if (type === 'query') {
            lan = "graphql";
            originalCode = oldSchema.source;
            modifiedCode = newSchema.source;
        } else if (type === 'page') {
            lan = "json";
            originalCode = JSON.stringify(JSON.parse(oldSchema.components), null, 2);
            modifiedCode = JSON.stringify(JSON.parse(newSchema.components), null, 2);
        } else {
            lan = "json";
            originalCode = JSON.stringify(oldSchema, null, 2);
            modifiedCode = JSON.stringify(newSchema, null, 2);
        }

        diffEditor.setModel({
            original: monaco.editor.createModel(originalCode, lan),
            modified: monaco.editor.createModel(modifiedCode, lan),
        });
    });
}
