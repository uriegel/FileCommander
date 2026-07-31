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
    if (res.itemsResult) {
        if (res.itemsResult.columns) {
            // TODO code replic
            const cols = res.itemsResult.columns.map(n => n.name)
            columnCount = cols.length
            tableView.setColumns(cols)
        }
        tableView.setItems(res.itemsResult.items)
        tableView.setPosition(res.itemsResult.pos)
    }
})

async function onKeyDown(evt) {
    if (evt.key == "Tab") {
        evt.preventDefault()
        evt.stopPropagation()
        const response = await fetch("request/tab")
    }
    else if (evt.ctrlKey && evt.key == "h") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/toggleHidden")
    }
}

document.addEventListener("keydown", evt => onKeyDown(evt))

init()

async function onEvent(evt) {
    console.log("Event", evt)
    if (evt.refresh) {
        const response = await fetch(`request/refresh/${0}`)
        const res = await response.json()
        if (res.itemsResult) {
            tableView.setItems(res.itemsResult.items)
            tableView.setPosition(res.itemsResult.pos)
        }
    }
}

async function init() {
    window.chrome.webview.addEventListener('message', event => onEvent(event.data))
    const response = await fetch("request/init")
    const itemsResult = await response.json()
    if (itemsResult.columns) {
        const cols = itemsResult.columns.map(n => n.name)
        columnCount = cols.length
        tableView.setColumns(cols)
    }
    tableView.setItems(itemsResult.items)
    tableView.setPosition(itemsResult.pos)
}


tableView.focus()
