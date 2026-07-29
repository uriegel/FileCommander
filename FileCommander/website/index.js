import './virtualtable/index.js'

let columnCount = 0

const tableView = document.getElementById("virtual-table")
const fill = document.getElementById("fill")

tableView.addEventListener("create-rowitem", evt => {
    const template = document.getElementById('item')
    const tr = template.content.cloneNode(true).firstElementChild
    for (let i = 0; i < columnCount - 1; i++) {
        const td = document.createElement("td")
        td.id = `item${i}`
        tr.appendChild(td)
    }
    evt.detail.tr = tr
})
tableView.addEventListener("measure-rowitem", evt => {
    const tr = evt.detail.tr
    const sp = tr.querySelector('#text')
    sp.textContent = 'Measuring...'
})
tableView.addEventListener("render-rowitem", evt => {
    const tr = evt.detail.tr
    const img = tr.querySelector('#img')
    img.src = evt.detail.item.icon
    const sp = tr.querySelector('#text')
    sp.textContent = evt.detail.item.text
    for (let i = 0; i < columnCount - 1; i++) {
        const element = tr.querySelector(`#item${i}`)
        element.textContent = evt.detail.item.values[i]
    }
})

tableView.addEventListener("process-selected", async evt => {
    const response = await fetch(`request/process/${evt.detail.pos}`)
    const res = await response.json()
    if (res.newItems) {
        const getItems = async () => {
            const response = await fetch("request/getItems")
            const items = await response.json()
            tableView.setItems(items.items)
            tableView.setPosition(items.pos)
        }
        getItems()
    }
})

async function onKeyDown(evt) {
    if (evt.key == "Tab") {
        evt.preventDefault()
        evt.stopPropagation()
        const response = await fetch("request/tab")
    }
}

document.addEventListener("keydown", evt => onKeyDown(evt))

init()

function onEvent(evt) {
    console.log("Event", evt)
    if (evt.columnsChanged) {
        const cols = evt.columnsChanged.columns.map(n => n.name)
        columnCount = cols.length
        tableView.setColumns(cols)
        const getItems = async () => {
            const response = await fetch("request/getItems")
            const items = await response.json()
            tableView.setItems(items.items)
            tableView.setPosition(items.pos)
        }
        getItems()
    }
}

async function init() {
    window.chrome.webview.addEventListener('message', event => onEvent(event.data))
    const response = await fetch("request/init")
}


tableView.focus()
