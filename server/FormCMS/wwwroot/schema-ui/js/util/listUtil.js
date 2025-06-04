import { del, publish } from "../services/services.js";
import { queryKeys, schemaTypes } from "../models/types.js";
import { hideOverlay, showOverlay } from "./loadingOverlay.js";

export async function renderSchemaTable(tblContainer, errContainer, loadData, actions) {
    showOverlay();
    const { data, error } = await loadData();
    hideOverlay();

    const errorPanel = typeof errContainer === 'string'
        ? document.querySelector(errContainer)
        : errContainer;

    if (error) {
        errorPanel.textContent = error;
        errorPanel.style.display = 'block';
        return;
    }

    const tableWrapper = typeof tblContainer === 'string'
        ? document.querySelector(tblContainer)
        : tblContainer;

    tableWrapper.innerHTML = `
        <table id="data-table" class="table table-bordered">
            <tbody id="table-body">
                <tr>
                    <td>ID</td>
                    <td>Type</td>
                    <td>Name</td>
                    <td>Status</td>
                    <td>Created At</td>
                    <td>Actions</td>
                </tr>
            </tbody>
        </table>`;

    const tbody = document.getElementById('table-body');
    const { showDelete, showViewHistory, showDiff } = actions;

    data.forEach((item, i) => {
        let url = "";
        let duplicateUrl = "";
        switch (item.type) {
            case schemaTypes.page:
                url = `page.html?${queryKeys.id}=${item.id}`;
                duplicateUrl = `page.html?${queryKeys.refId}=${item.id}`;
                break;
            case schemaTypes.query:
                url = item.settings.query.ideUrl;
                duplicateUrl = item.settings.query.ideUrl.replace("query%20" + item.name, "query%20" + item.name + "Copy");
                break;
            default:
                url = `edit.html?${queryKeys.type}=${item.type}&${queryKeys.id}=${item.id}`;
                duplicateUrl = `edit.html?type=${item.type}&${queryKeys.refId}=${item.id}`;
                break;
        }

        const publishButton = item.publicationStatus === 'published' ? '' :
            `<button class="btn badge btn-primary btn-sm publish-schema" id="${item.id}" data-schema-id="${item.schemaId}">Publish</button>`;

        const deleteButton = showDelete ?
            `<button class="btn badge btn-danger btn-sm delete-schema" id="${item.id}" data-name="${item.name}">Delete</button>` : '';

        const historyButton = showViewHistory ?
            `<a href="history.html?${queryKeys.schemaId}=${item.schemaId}&${queryKeys.type}=${item.type}"  
                class="btn btn-secondary btn-sm badge">View History</a>` : '';

        const diffButton = showDiff && i > 0 ?
            `<a href="diff.html?${queryKeys.schemaId}=${item.schemaId}&${queryKeys.type}=${item.type}&${queryKeys.oldId}=${item.id}&${queryKeys.newId}=${data[0].id}" 
                class="btn btn-secondary btn-sm badge">Diff</a>` : '';

        const duplicateButton = item.type !== schemaTypes.menu ?
            `<a href="${duplicateUrl}" class="btn btn-secondary btn-sm badge">Duplicate</a>` : '';

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${item.id}</td>
            <td>${item.type}</td>
            <td><a href="${url}">${item.name}</a></td>
            <td>${item.publicationStatus}</td>
            <td>${item.createdAt}</td>
            <td>${publishButton} ${deleteButton} ${historyButton} ${diffButton} ${duplicateButton}</td>
        `;

        tbody.appendChild(row);
    });

    document.querySelectorAll('.delete-schema').forEach(btn => {
        btn.addEventListener('click', handleDelete);
    });

    document.querySelectorAll('.publish-schema').forEach(btn => {
        btn.addEventListener('click', handlePublish);
    });
    
    async function handleDelete(e) {
        const btn = e.currentTarget;
        if (confirm("Do you want to delete schema: " + btn.getAttribute('data-name'))) {
            showOverlay();
            const { error } = await del(btn.id);
            hideOverlay();
            if (error) {
                errorPanel.textContent = error;
                errorPanel.style.display = 'block';
            } else {
                window.location.reload(); 
            }
        }
    }

    async function handlePublish(e) {
        const btn = e.currentTarget;
        const id = btn.id;
        const schemaId = btn.getAttribute('data-schema-id');
        const { error } = await publish({ id, schemaId });

        if (error) {
            errorPanel.textContent = error;
            errorPanel.style.display = 'block';
        } else {
            window.location.reload();
        }
    }
}
