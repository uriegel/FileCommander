import './virtualtable/index.js'

const tableView = document.getElementById("virtual-table")
const fill = document.getElementById("fill")
const restriction = document.getElementById("restriction")
tableView.setStylesheet("styles/tableview.css")
tableView.setOnColumnWidthChange(onColumnWidthChange)
tableView.setOnSort(onSort)
tableView.focus()

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
    if (evt.detail.item.hidden)
        tr.classList.add("isHidden")
    else
        tr.classList.remove("isHidden")
    if (evt.detail.item?.exifValue?.date)
        tr.classList.add("exif")
    else
        tr.classList.remove("exif")
    const img = tr.querySelector('#img')
    img.src = evt.detail.item.icon
    const sp = tr.querySelector('#text')
    sp.textContent = evt.detail.item.text
    for (let i = 0; i < columnCount; i++) {
        const element = tr.querySelector(`#item${i}`)
        if (i == 0 && evt.detail.item?.exifValue?.date) 
            element.textContent = evt.detail.item?.exifValue?.date
        else if (evt.detail.item.values.length > i)
            element.textContent = evt.detail.item.values[i]
    }
})

tableView.addEventListener("position-changed", async evt => {
    const response = await fetch(`request/onposition/${getPosition(evt.detail.pos)}`)
})

tableView.addEventListener("process-selected", async evt => {
    const response = await fetch(`request/process/${getPosition(evt.detail.pos)}`)
    const res = await response.json()
    if (res.itemsResult) {
        stopRestriction()
        checkColumns(res.itemsResult.columns)
        setItems(res.itemsResult.items)
        tableView.setPosition(res.itemsResult.pos)
    }
})

/**
 * 
 * @param {KeyboardEvent} evt
 */
async function onKeyDown(evt) {
    if (evt.key == "Tab") {
        evt.preventDefault()
        evt.stopPropagation()
        const response = await fetch(`request/tab${evt.shiftKey ? "?shift=true" : ""}`)
    }
    else if (evt.ctrlKey && evt.key == "h") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/toggleHidden")
    }
    else if (evt.ctrlKey && evt.key == "r") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/refresh")
    }
    else if (evt.key == "Escape") 
        stopRestriction()
    else if (evt.key == "Backspace") {
        if (!unrestrictedItems) {
            const response = await fetch(`request/history?forward=${evt.shiftKey}`)
            const itemsResult = await response.json()
            checkColumns(itemsResult.columns)
            setItems(itemsResult.items)
        }
        else {
            restriction.value = restriction.value.slice(0, -1)
            if (!restriction.value)
                stopRestriction()
            else {
                const restricted = unrestrictedItems.filter(n => n.text.toLowerCase().startsWith(restriction.value))
                tableView.setItems(restricted, 0)
            }
        }
    }
    else if (evt.key.length == 1) {
        if (!unrestrictedItems) {
            const items = tableView.getItems()
            const restricted = items.filter(n => n.text.toLowerCase().startsWith(evt.key))
            if (restricted.length > 0) {
                unrestrictedItems = items
                tableView.setItems(restricted, 0)
                restriction.classList.add("show")
                restriction.value += evt.key
            }
        } else {
            const restricted = unrestrictedItems.filter(n => n.text.toLowerCase().startsWith(restriction.value + evt.key))
            if (restricted.length > 0) {
                tableView.setItems(restricted, 0)
                restriction.value += evt.key
            }
        }
    }
}

document.addEventListener("keydown", evt => onKeyDown(evt))

init()

async function onEvent(evt) {
    console.log("Event", evt)
    if (evt.refresh) {
        stopRestriction() 
        await refresh()
    }
    if (evt.reload) {
        stopRestriction()
        const response = await fetch(`request/reload/${getPosition()}`)
        const res = await response.json()
        if (res.itemsResult) {
            setItems(res.itemsResult.items)
            tableView.setPosition(res.itemsResult.pos)
        }
    }
    if (evt.changePath) {
        stopRestriction()
        const response = await fetch(`request/changePath?path=${evt.changePath.path}`)
        const res = await response.json()
        if (res.itemsResult) 
            setItems(res.itemsResult.items)
    }
}

async function init() {
    window.chrome.webview.addEventListener('message', event => onEvent(event.data))
    const response = await fetch("request/init")
    const itemsResult = await response.json()
    checkColumns(itemsResult.columns)
    setItems(itemsResult.items)
    tableView.setPosition(itemsResult.pos)
}

function checkColumns(columns) {
    if (columns) {
        columnCount = columns.length
        tableView.setColumns(columns)
    }
}

function onColumnWidthChange(cols) {
    console.log("On width change", cols)
    //localStorage.setItem("columnWidths", JSON.stringify(cols))
}

function getPosition(pos) {
    if (!pos)
        pos = tableView.getPosition()
    if (!unrestrictedItems)
        return pos;
    else {
        const text = tableView.getItems()[pos].text
        console.log("Text", text)
        return unrestrictedItems.findIndex(n => n.text == text)
    }
}

function stopRestriction() {
    if (unrestrictedItems) {
        restriction.value = ""
        tableView.setItems(unrestrictedItems, 0)
        unrestrictedItems = null
        restriction.classList.remove("show")
    }
}

async function onSort(e) {
    stopRestriction()
    const response = await fetch(`request/sort?column=${e.index}&descending=${e.descending}${e.subColumn ? "&subcolumn=true" : ""}&pos=${tableView.getPosition()}`)
    const res = await response.json()
    if (res.itemsResult) {
        tableView.setItems(res.itemsResult.items)
        tableView.setPosition(res.itemsResult.pos)
    }
}

async function refresh() {
    const response = await fetch(`request/refresh/${getPosition()}`)
    const res = await response.json()
    if (res.itemsResult) {
        tableView.setItems(res.itemsResult.items)
        tableView.setPosition(res.itemsResult.pos)
    }
}

async function setItems(items) {
    tableView.setItems(items)
    itemsMap = new Map(items.map(item => [item.text, item]))
    changeDetectionCancellation = true
    detectChanges()
}

async function detectChanges() {
    changeDetectionCancellation = false
    while (!changeDetectionCancellation)
    { 
        const response = await fetch("request/getFileChanges")
        const res = await response.json()
        console.log("Changes", res)
        if (res.changes == undefined)
            break

        res.changes.forEach(n => {
            if (n.item) {
                const item = itemsMap.get(n.item.text)
                if (item && n.item.exifValue)
                    item.exifValue = n.item.exifValue
                if (item && n.item.values.length == 3 && n.item.values[2].length > 0)
                    item.values = n.item.values
                tableView.refresh()
            } else if (n.deleted) {
                if (!unrestrictedItems) {
                    const items = tableView.getItems()
                    tableView.setItems(items.filter((_, i) => i != n.deleted.position))
                    tableView.setPosition(n.deleted.selection)
                }
            } else if (n.created) {
                if (!unrestrictedItems) {
                    const items = tableView.getItems()
                    tableView.setItems([...items.slice(0, n.created.position), n.created.item, ...items.slice(n.created.position)])
                    tableView.setPosition(n.created.selection)
                }
            }
        })
        await delayAsync(40) 
    }
}

function delayAsync(ms) {
    return new Promise(res => {
        setTimeout(res, ms)
    })
}

var itemsMap
var columnCount = 0
var unrestrictedItems
var changeDetectionCancellation = false