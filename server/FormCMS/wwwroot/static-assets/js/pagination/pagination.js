import {getPart} from "../services/pageService.js";

export function renderPagination(element, render) {
   if (element !== document){
       setPaginationStatus(element, render);
   }
    element.querySelectorAll('[data-component="data-list"]').forEach(list=>setPaginationStatus(list,render));
}

export async function reloadDataList(dataList, render) {
    const each = dataList.querySelector('[data-component="foreach"]');
    const recordId = each.getAttribute('__record_id');
    const res = await singleFlyGetPart(each.id, recordId, null, null,  render);
    if (res.error) {
        console.log(res.error);
    }
    each.innerHTML = res;
}

export function  setPaginationStatus(list, render) {
    const nav = list.querySelector(':scope > [data-component="pagination"]');
    if (!nav) return;

    const each = list.querySelector(':scope > [data-component="foreach"]');
    const nodeId = each.id;
    const recordId = each.getAttribute('__record_id');
    const pagination = list.getAttribute('pagination');

    switch (pagination) {
        case 'Button':
            initButton();
            break;
        case 'InfiniteScroll':
            nav.innerHTML = '';
            initObserver();
            break;
        case 'None':
            nav.style.display = 'none';
            break;
    }

    function initObserver(){
        const observer = new IntersectionObserver(async entries => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    const lastEle = each.lastElementChild;
                    if (!lastEle) return;
                    const last = lastEle.getAttribute('cursor');
                    if (!last) return;

                    const res = await singleFlyGetPart(nodeId, recordId, null, last);
                    if (!res || res.error) {
                        console.log(res);
                    }else {
                        each.insertAdjacentHTML('beforeend', res);
                        render(list)
                    }
                }
            }
        });
        observer.observe(nav);
    }

    function initButton() {
        const pre = nav.querySelector('[data-component="previous"]');
        const next = nav.querySelector('[data-component="next"]');

        pre.addEventListener('click', () => handlePagination(false))
        next.addEventListener('click', () => handlePagination(true))
        togglePagination();

        function togglePagination() {
            const first = each.firstElementChild && each.firstElementChild.getAttribute('cursor')
            const last = each.lastElementChild && each.lastElementChild.getAttribute('cursor')
            pre.style.display = first ? 'inline' : 'none';
            next.style.display = last ? 'inline' : 'none';
        }

        async function handlePagination(isNext) {
            //have to get first last each time after each-node changed
            const first = isNext ? null : each.firstElementChild.getAttribute('cursor')
            const last = isNext ? each.lastElementChild.getAttribute('cursor') : null;
            const html = await singleFlyGetPart(nodeId, recordId, first, last);
            each.innerHTML = html;
            render(list);
        }
    }
}

const loadingDict = new Map();
async function singleFlyGetPart(node,source,first,last) {
    const key = node + source??'' + first??'' + last??'' ;
    if (!node || loadingDict.has(key)) {
        return; // Already loading
    }
    loadingDict.set(key, true);
    try {
        return  await getPart(node,source,first,last);
    } catch (error) {
        console.error('Error loading more.', error);
    } finally {
        loadingDict.delete(key);
    }
}
