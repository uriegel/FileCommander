import './virtualtable/index.js'

const tableView = document.getElementById("virtual-table")
const fill = document.getElementById("fill")

tableView.setColumns([
    "Name",
    "Date",
    "Size"
])

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

async function init() {
    const response = await fetch("request/getItems")
    const data = await response.json()
    console.log("request arrives", data)
    tableView.setItems(data)
}


tableView.focus()
