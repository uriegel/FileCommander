import './virtualtable/index.js'

const tableView = document.getElementById("virtual-table")
const fill = document.getElementById("fill")

tableView.addEventListener("create-rowitem", evt => {
    const template = document.getElementById('item')
    const tr = template.content.cloneNode(true).firstElementChild
    evt.detail.tr = tr
})
tableView.addEventListener("measure-rowitem", evt => {
    const tr = evt.detail.tr
    const img = tr.querySelector('#img')
    img.src = "image/icon5"
    const sp = tr.querySelector('#text')
    sp.textContent = 'Measuring...'
})
tableView.addEventListener("render-rowitem", evt => {
    const tr = evt.detail.tr
    const img = tr.querySelector('#img')
    img.src = evt.detail.item.icon
    const sp = tr.querySelector('#text')
    sp.textContent = evt.detail.item.name
    const element2 = tr.querySelector('#item2')
    element2.textContent = evt.detail.item.date
    const element3 = tr.querySelector('#item3')
    element3.textContent = evt.detail.item.size
})

init()

function onEvent(evt) {
    console.log("Event", evt)
    if (evt.getItems) {
        tableView.setColumns(evt.getItems.columns.map(n => n.name))
        const getItems = async () => {
            const response = await fetch("request/getItems")
            const data = await response.json()
            tableView.setItems(data)
        }
        getItems()
    }
}

async function init() {
    window.chrome.webview.addEventListener('message', event => onEvent(event.data))
    const response = await fetch("request/init")
}


tableView.focus()
