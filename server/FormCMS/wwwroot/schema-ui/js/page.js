import { loadEditor } from "../grapes-components/grapes.js";
import { checkUser } from "./util/checkUser.js";
import { loadNavBar } from "./components/navbar.js";
import { single, save } from "./services/services.js";
import { queryKeys, schemaTypes } from "./models/types.js";
import { getParams } from "./util/searchParamUtil.js";
import { hideOverlay, showOverlay } from "./util/loadingOverlay.js";

let schema;
let editor;

const [navBox, headerBox, inputsBox, grapesBox, errorBox] = [
    document.querySelector("#nav-box"),
    document.querySelector("#header-box"),
    document.querySelector("#inputs-box"),
    document.querySelector("#grapes-box"),
    document.querySelector("#error-box")
];

const [id, refId] = getParams([queryKeys.id, queryKeys.refId]);
loadNavBar(navBox);
addActionButtons();
addInputs();
checkUser(() => init(id, refId));

async function init(id, refId) {
    if (!id && !refId) {
        editor = loadEditor(grapesBox);
        return;
    }

    const schemaData = await loadSchema(id || refId);
    if (schemaData) {
        const pageData = schemaData.settings[schemaTypes.page];
        if (id) {
            schema = schemaData;
        } else {
            schema = { type: schemaTypes.page, settings: { [schemaTypes.page]: pageData } };
        }
        restoreFormData(pageData);
        editor = loadEditor(grapesBox, JSON.parse(pageData.components), JSON.parse(pageData.styles));
    }
}

async function handleSave(publish) {
    const payload = {
        html: editor.getHtml(),
        css: editor.getCss(),
        components: JSON.stringify(editor.getComponents()),
        styles: JSON.stringify(editor.getStyle()),
        type: schemaTypes.page
    };

    if (!attachFormData(payload)) {
        return false;
    }

    schema = schema || { type: schemaTypes.page };
    schema.settings = { page: payload };

    showOverlay();
    const { data, error } = await save(schema,publish);
    hideOverlay();

    if (data) {
        // Replace $.toast with a simple alert or custom toast logic
        alert("Submit succeeded!");

        schema = data;
        history.pushState(null, "", `page.html?${queryKeys.id}=${data.id}`);
        errorBox.textContent = "";
        errorBox.style.display = "none";
    } else {
        errorBox.textContent = error;
        errorBox.style.display = "block";
    }
}

async function loadSchema(idVal) {
    if (!idVal) return null;

    showOverlay();
    const { data: schemaData, error } = await single(idVal);
    hideOverlay();

    if (error) {
        errorBox.textContent = error;
        errorBox.style.display = "block";
        return null;
    }

    return schemaData;
}

function addInputs() {
    inputsBox.innerHTML = `
    <div class="row">
      <div class="col-md-4 mb-3">
        <label for="name" class="form-label">Page Name</label>
        <input id="name" type="text" class="form-control" required>
      </div>
      <div class="col-md-4 mb-3">
        <label for="title" class="form-label">Page Title</label>
        <input id="title" type="text" class="form-control" required>
      </div>
      <div class="col-md-4 mb-3">
        <label for="query" class="form-label">Query</label>
        <input id="query" type="text" class="form-control">
      </div>
    </div>
  `;
}

const controls = ['name', 'title', 'query'];

function restoreFormData(payload) {
    for (const ctl of controls) {
        const el = document.getElementById(ctl);
        if (el) el.value = payload[ctl] || "";
    }
}

function attachFormData(payload) {
    for (const ctlName of controls) {
        const el = document.getElementById(ctlName);
        if (el.required && !el.value) {
            errorBox.textContent = `${ctlName} cannot be empty`;
            errorBox.style.display = "block";
            return false;
        }
        payload[ctlName] = el.value;
    }
    return true;
}

function addActionButtons() {
    headerBox.innerHTML = `
    <button id="savePage" class="btn btn-primary">Save Page</button>
    <button id="saveAndPublish" class="btn btn-primary">Save and Publish</button>
    <button id="visitPage" class="btn btn-primary">View Page</button>
  `;

    const saveBtn = document.getElementById("savePage");
    const saveAndPublish = document.getElementById("saveAndPublish");
    const visitBtn = document.getElementById("visitPage");

    saveBtn.addEventListener("click", ()=>handleSave(false));
    saveAndPublish.addEventListener("click", ()=>handleSave(true));
    visitBtn.addEventListener("click", () => {
        const name = document.getElementById("name").value;
        if (name) {
            window.open(`/${name}/?${queryKeys.sandbox}=1`, "_blank");
        }
    });
}
