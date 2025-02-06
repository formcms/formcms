import {del, publish} from "./repo.js";

async function handleDelete(e) {
    if (confirm("Do you want to delete schema: " + e.getAttribute('data-name'))) {
        $.LoadingOverlay("show");
        const { error} =await del(e.id);
        $.LoadingOverlay("hide");
        if (error){
            $('#errorPanel').text(error).show();
        }else {
            window.location.reload();
        }
    }
}

async function handlePublish(e) {
    const id = e.id;
    const schemaId = e.getAttribute('data-schema-id')
    const {error} = await publish({id, schemaId});
    if (error){
        $('#errorPanel').text(error).show();
    }else {
        window.location.reload();
    }
}

export async function renderSchemaTable(elementId, loadData, actions) {
    $.LoadingOverlay("show");
    const {data, error} =await loadData();
    $.LoadingOverlay("hide");

    if (error) { 
        $('#errorPanel').text(error).show();
        return;
    }
    const {showDelete, showViewHistory} = actions;
    
    const $ele = $('#' + elementId);
    $ele.html(`<table id="data-table" class="table table-bordered">
        <tbody id="table-body">
        <tr>
            <td>ID</td>
            <td>SchemaId</td>
            <td>Type</td>
            <td>Name</td>
            <td>Status</td>
            <td>Created At</td>
            <td>Actions</td>
        </tr>
        </tbody>
    </table>`);
    
    data.forEach(item => {
        let url = ""
        switch (item.type) {
            case 'page':
                url = `page.html?id=${item.id}`;
                break;
            case 'query':
                url = item.settings.query.ideUrl;
                break;
            default:
                url = `edit.html?type=${item.type}&id=${item.id}`;
                break;
        }

        const publishButton = item.publicationStatus === 'published'
            ? ''
            : `<button
                    class="btn badge btn-primary btn-sm delete-btn"
                    id="${item.id}"
                    onclick="handlePublish(this)"
                    data-schema-id="${item.schemaId}">Publish
                </button>`

        const deleteButton = showDelete
            ? `<button class="btn badge btn-danger btn-sm delete-btn" id="${item.id}" onclick="handleDelete(this)"
                      data-name="${item.name}">Delete
                </button>`
            : '';

        const historyButton = showViewHistory 
            ? `<a href="history.html?schemaId=${item.schemaId}"> 
                    <button class="btn btn-primary btn-sm badge" id="${item.id}" data-name="${item.name}">View History</button>
               </a>`
            :'';
        
        const row = 
            `<tr>
                        <td>${item.id}</td>
                        <td>${item.schemaId}</td>
                        <td>${item.type}</td>
                        <td><a href="${url}">${item.name}</a></td>
                        <td>${item.publicationStatus}</td>
                        <td>${item.createdAt}</td>
                        <td>${publishButton}  ${deleteButton} ${historyButton} </td>
                        </tr>
            `;
        $('#table-body').append(row);
    });
}