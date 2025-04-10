$(document).ready(function () {
    let loadingDict = new Map();
    $('[data-command="previous"]').click(e => handlePaginationButton(e, e.target, false));
    $('[data-command="next"]').click(e => handlePaginationButton(e, e.target, true));

    initIntersectionObserver();
    setPaginationStatus();

    function setPaginationStatus() {
        $('[data-source="data-list"]').each(function () {
            let pagination = $(this).attr('pagination');
            let first = $(this).attr('first');
            let last = $(this).attr('last');

            let nav = $(this).parent().find(':has([data-command="previous"])');
            if (pagination !== 'Button' || !first && !last) {
                nav.remove();
            } else {
                if (first && first.length > 0) {
                    nav.find('[data-command="previous"]').show();
                } else {
                    nav.find('[data-command="previous"]').hide();
                }

                if (last && last.length > 0) {
                    nav.find('[data-command="next"]').show();
                } else {
                    nav.find('[data-command="next"]').hide();
                }
            }
        });
    }

    function handlePaginationButton(event, button, isNext) {
        event.preventDefault();
        let container = button.parentElement.parentElement;
        let list = container.querySelector('[data-source="data-list"]');
        loadMore(list.attributes[isNext ? "last" : "first"].value, response => {
            list.outerHTML = response;
            setPaginationStatus();
        })
    }

    function loadMore(token, render) {
        if (!token || loadingDict[token]) {
            return
        }
        loadingDict[token] = true;
        $.ajax({
            url: '/page_part',
            type: 'GET',
            data: {
                token,
            },
            success: function (response) {
                render(response);
                loadingDict[token] = false;
            },
            error: function () {
                console.log('Error loading more.');
                loadingDict[token] = false;
            }
        });
    }

    function initIntersectionObserver() {
        let observer = new IntersectionObserver(function (entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    loadMore(entry.target.attributes['last'].value, response => {
                        const template = document.createElement('template');
                        template.innerHTML = response.trim();
                        entry.target.parentElement.appendChild(template.content);
                        entry.target.remove();
                        initIntersectionObserver();
                    });
                }
            });
        });

        // Observe the hidden element
        let ele = document.querySelector(".load-more-trigger");
        if (ele) observer.observe(ele);
    }
});
