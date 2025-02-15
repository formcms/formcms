import {del, publish} from "./repo.js";
import {queryKeys, schemaTypes} from "./types.js";

async function handleDelete() {
    if (confirm("Do you want to delete schema: " + this.getAttribute('data-name'))) {
        $.LoadingOverlay("show");
        const { error} =await del(this.id);
        $.LoadingOverlay("hide");
        if (error){
            $('#errorPanel').text(error).show();
        }else {
            window.location.reload();
        }
    }
}

async function handlePublish() {
    const id = this.id;
    const schemaId = this.getAttribute('data-schema-id')
    const {error} = await publish({id, schemaId});
    if (error){
        $('#errorPanel').text(error).show();
    }else {
        window.location.reload();
    }
}

export async function renderSchemaTable(tblContainer,errContainer, loadData, actions) {
    $.LoadingOverlay("show");
    const {data, error} =await loadData();
    $.LoadingOverlay("hide");

    if (error) { 
        $(errContainer).text(error).show();
        return;
    }
    
    const {showDelete, showViewHistory, showDiff} = actions;
    
    $(tblContainer).html(`<table id="data-table" class="table table-bordered">
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
    </table>`);
    
    data.forEach((item,i) => {
        let url = "";
        let duplicateUrl = "";
        switch (item.type) {
            case schemaTypes.page:
                url = `page.html?${queryKeys.id}=${item.id}`;
                duplicateUrl = `page.html?${queryKeys.refId}=${item.id}`;
                break;
            case schemaTypes.query:
                url = item.settings.query.ideUrl;
                duplicateUrl = item.settings.query.ideUrl.replace("query%20"+item.name, "query%20"+item.name + "Copy");
                break;
            default:
                url = `edit.html?${queryKeys.type}=${item.type}&${queryKeys.id}=${item.id}`;
                duplicateUrl = `edit.html?type=${item.type}&${queryKeys.refId}=${item.id}`;
                break;
        }

        const publishButton = item.publicationStatus === 'published'
            ? ''
            : `<button class="btn badge btn-primary btn-sm publish-schema" id="${item.id}" data-schema-id="${item.schemaId}">Publish</button>`

        const deleteButton = showDelete
            ? `<button class="btn badge btn-danger btn-sm delete-schema" id="${item.id}" data-name="${item.name}">Delete</button>`
            : '';

        const historyButton = showViewHistory
            ? `<a href="history.html?schemaId=${item.schemaId}"  class="btn btn-secondary btn-sm badge">View History</a>`
            : '';

        const diffButton = showDiff && i > 0
            ? `<a href="diff.html?id1=${item.id}&id2=${data[0].id}" class="btn btn-secondary btn-sm badge">Diff</a>`
            : '';

        const duplicateButton = item.type !== schemaTypes.menu 
            ?`<a href="${duplicateUrl}" class="btn btn-secondary btn-sm badge">Duplicate</a>`
            :'';
        const row =
            `<tr>
                        <td>${item.id}</td>
                        <td>${item.type}</td>
                        <td><a href="${url}">${item.name}</a></td>
                        <td>${item.publicationStatus}</td>
                        <td>${item.createdAt}</td>
                        <td>${publishButton}  ${deleteButton} ${historyButton} ${diffButton} ${duplicateButton}</td>
                        </tr>
            `;
        $('#table-body').append(row);
    });
    $('.delete-schema').on('click', handleDelete);
    $('.publish-schema').on('click',handlePublish);
}