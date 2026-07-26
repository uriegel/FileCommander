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
    const number = evt.detail.item.slice(-1)
    const img = tr.querySelector('#img')
    img.src = `image/icon${number}`
    const sp = tr.querySelector('#text')
    sp.textContent = evt.detail.item
    const element2 = tr.querySelector('#item2')
    element2.textContent = `Item 2 - ${evt.detail.item}`
    const element3 = tr.querySelector('#item3')
    element3.textContent = `Item 3 - ${evt.detail.item}`
})

tableView.setItems(Array.from({ length: 70_000 }, (value, index) => `Eintrag Numero: ${index}`))
tableView.focus()
