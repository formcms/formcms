import {fetchPagePart} from "../utils/datalist.js";

initPaginition();

function initPaginition() {
    document.querySelectorAll('[data-component="data-list"]').forEach(list => {
        const nav = list.querySelector('[data-component="pagination"]');
        if (!nav) return;
        
        const each = list.querySelector('[data-component="foreach"]');
        const nodeId = each.id;
        const recordId = each.getAttribute('__record_id');
        
        const pagination = list.getAttribute('pagination');
        
        switch (pagination) {
            case 'Button':
                initButton();
                break;
            case 'InfiniteScroll':
                initObserver();
                break;
            case 'None':
                nav.style.display = 'none';
                break;
        }
        
        function initObserver(){
            nav.innerHTML = '';
            const observer = new IntersectionObserver(async entries => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        const last = each.lastElementChild.getAttribute('cursor');
                        if (last) {
                            const html = await fetchPagePart(nodeId, recordId, null, last);
                            each.insertAdjacentHTML('beforeend', html);
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
                const first = each.firstElementChild.getAttribute('cursor')
                const last = each.lastElementChild.getAttribute('cursor')
                pre.style.display = first ? 'inline' : 'none';
                next.style.display = last ? 'inline' : 'none';
            }

            async function handlePagination(isNext) {
                //have to get first last each time after each-node changed
                const first = isNext ? null : each.firstElementChild.getAttribute('cursor')
                const last = isNext ? each.lastElementChild.getAttribute('cursor') : null;
                const html = await fetchPagePart(nodeId, recordId, first, last);
                each.innerHTML = html;
                togglePagination();
            }
        }
    });
}