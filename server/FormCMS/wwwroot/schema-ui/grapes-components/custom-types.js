export const customTypes = {
    ['data-list']: [
        {name:"field", label:'field'},
        {name:"query", label:'query'},
        {name:"qs", label:'qs'},
        {name:"offset", label:'offset'},
        {name:"limit", label:'limit'},
        {
            type: 'select',
            label: 'Pagination',
            name: 'pagination',
            options: [
                { value: 'None', name: 'None' },
                { value: 'Button', name: 'Button' },
                { value: 'InfiniteScroll', name: 'Infinite Scroll' },
            ],
        }
    ],
    ['activity-bar']:[
        {name:"entityName", label:'Entity Name'},
        {name:"recordId", label:'Record Id'},
    ]
};