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
    if (evt.detail.item.selected)
        tr.classList.add("selected")
    else
        tr.classList.remove("selected")
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
        setItems(res.itemsResult.items, true)
        tableView.setPosition(res.itemsResult.pos)
    }
})

tableView.addEventListener("mousedown", evt => {
    if (evt.ctrlKey) {
        const items = tableView.getItems()
        const pos = tableView.getPosition()
        const selItem = items[pos]
        if (selItem.isSelectable)
            selItem.selected = !selItem.selected
        tableView.refresh()
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
    else if (evt.key == "F1") {
        evt.preventDefault();
        evt.stopPropagation()
        showFavorites()
    }
    else if (evt.key == "F2") {
        evt.preventDefault();
        evt.stopPropagation()
        rename(evt.shiftKey)
    }
    else if (evt.key == "F5") {
        evt.preventDefault();
        evt.stopPropagation()
        copy(false)
    }
    else if (evt.key == "F6") {
        evt.preventDefault();
        evt.stopPropagation()
        copy(true)
    }
    else if (evt.key == "F7") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/createFolder")
    }
    else if (evt.key == "F9") {
        evt.preventDefault();
        evt.stopPropagation()
        adaptPath()
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
                tableView.setItems(restricted)
            }
        }
    }
    else if (evt.key == "Insert") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/toggleSelection")
    }
    else if (evt.key == "Home" && evt.shiftKey) {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/selectAllAbove")
    }
    else if (evt.key == "End" && evt.shiftKey) {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/selectAllBeneath")
    }
    else if (evt.code == "NumpadAdd") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/selectAll")
    }
    else if (evt.code == "NumpadSubtract") {
        evt.preventDefault();
        evt.stopPropagation()
        await fetch("request/command/selectNone")
    }
    else if (evt.code == "Delete")
        deleteItems()
    else if (evt.code == "Enter" && evt.altKey)
        showProperties()
    else if (evt.code == "Enter" && evt.ctrlKey)
        openWith()
    else if (evt.key.length == 1) {
        if (!unrestrictedItems) {
            const items = tableView.getItems()
            const restricted = items.filter(n => n.text.toLowerCase().startsWith(evt.key))
            if (restricted.length > 0) {
                unrestrictedItems = items
                tableView.setItems(restricted)
                restriction.classList.add("show")
                restriction.value += evt.key
            }
        } else {
            const restricted = unrestrictedItems.filter(n => n.text.toLowerCase().startsWith(restriction.value + evt.key))
            if (restricted.length > 0) {
                tableView.setItems(restricted)
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
            setItems(res.itemsResult.items, true)
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
    if (evt.toggleSelection) {
        const items = tableView.getItems()
        const pos = tableView.getPosition()
        const selItem = items[pos]
        if (selItem.isSelectable)
            selItem.selected = !selItem.selected
        tableView.refresh()
        tableView.setPosition(pos + 1)
    }
    if (evt.selectAllAbove) {
        const items = tableView.getItems()
        const pos = tableView.getPosition()
        items.forEach((n, i) => {
            if (n.isSelectable)
                n.selected = i <= pos
        })
        tableView.refresh()
    }
    if (evt.selectAllBeneath) {
        const items = tableView.getItems()
        const pos = tableView.getPosition()
        items.forEach((n, i) => {
            if (n.isSelectable)
                n.selected = i >= pos
        })
        tableView.refresh()
    }
    if (evt.selectAll) {
        const items = tableView.getItems()
        items.forEach(n => {
            if (n.isSelectable)
                n.selected = true
        })
        tableView.refresh()
    }
    if (evt.selectNone) {
        const items = tableView.getItems()
        items.forEach(n => {
            if (n.isSelectable)
                n.selected = false
        })
        tableView.refresh()
    }
    if (evt.deleteItems) 
        deleteItems()
    if (evt.rename)
        rename(false)
    if (evt.renameAsCopy)
        rename(true)
    if (evt.copy)
        copy(false)
    if (evt.move)
        copy(true)
    if (evt.adaptPath)
        adaptPath()
    if (evt.execute)
        execute()
    if (evt.showProperties)
        showProperties()
    if (evt.openWith)
        openWith()
    if (evt.showFavorites)
        showFavorites()
}

async function init() {
    window.chrome.webview.addEventListener('message', event => onEvent(event.data))
    const response = await fetch("request/init")
    const itemsResult = await response.json()
    checkColumns(itemsResult.columns)
    setItems(itemsResult.items, true)
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
        return unrestrictedItems.findIndex(n => n.text == text)
    }
}

function stopRestriction() {
    if (unrestrictedItems) {
        restriction.value = ""
        const pos = tableView.getPosition()
        const text = tableView.getItems()[pos].text
        const lastPos = Math.max(unrestrictedItems.findIndex(n => n.text == text), 0)
        tableView.setItems(unrestrictedItems, true)
        tableView.setPosition(lastPos)
        unrestrictedItems = null
        restriction.classList.remove("show")
    }
}

async function onSort(e) {
    stopRestriction()
    const response = await fetch(`request/sort?column=${e.index}&descending=${e.descending}${e.subColumn ? "&subcolumn=true" : ""}&pos=${tableView.getPosition()}`)
    const res = await response.json()
    if (res.itemsResult) {
        tableView.setItems(res.itemsResult.items, true)
        tableView.setPosition(res.itemsResult.pos)
    }
}

async function refresh() {
    const response = await fetch(`request/refresh/${getPosition()}`)
    const res = await response.json()
    if (res.itemsResult) {
        tableView.setItems(res.itemsResult.items, true)
        tableView.setPosition(res.itemsResult.pos)
    }
}

async function setItems(items) {
    tableView.setItems(items)
    itemsMap = createItemsMap(items)
    changeDetectionCancellation = true
    detectChanges()
}

function createItemsMap(items) {
    return new Map(items.map((item, idx) => [item.text, { item, idx }]))
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
                const item = itemsMap.get(n.item.text).item
                if (item && n.item.exifValue)
                    item.exifValue = n.item.exifValue
                if (item && n.item.values.length == 3)
                    item.values = n.item.values
            } else if (n.deleted) {
                if (!unrestrictedItems) {
                    let items = tableView.getItems()
                    items = items.filter((_, i) => i != n.deleted.position)
                    tableView.setItems(items, true)
                    itemsMap = createItemsMap(items)
                    tableView.setPosition(n.deleted.selection)
                } else {
                    unrestrictedItems = unrestrictedItems.filter((_, i) => i != n.deleted.position)
                    itemsMap = createItemsMap(unrestrictedItems)
                }
            } else if (n.created) {
                if (!unrestrictedItems) {
                    let items = tableView.getItems()
                    items = [...items.slice(0, n.created.position), n.created.item, ...items.slice(n.created.position)]
                    tableView.setItems(items, true)
                    itemsMap = createItemsMap(items)
                    tableView.setPosition(n.created.selection)
                } else {
                    unrestrictedItems = [...unrestrictedItems.slice(0, n.created.position), n.created.item, ...unrestrictedItems.slice(n.created.position)]
                    itemsMap = createItemsMap(unrestrictedItems)
                }
            } else if (n.renamed) {
                if (!unrestrictedItems) {
                    let items = tableView.getItems()
                    items = items.filter((_, i) => i != n.renamed.oldPosition)
                    items = [...items.slice(0, n.renamed.position), n.renamed.item, ...items.slice(n.renamed.position)]
                    tableView.setItems(items, true)
                    itemsMap = createItemsMap(items)
                    tableView.setPosition(n.renamed.selection)
                } else {
                    unrestrictedItems = unrestrictedItems.filter((_, i) => i != n.renamed.oldPosition)
                    unrestrictedItems = [...unrestrictedItems.slice(0, n.renamed.position), n.renamed.item, ...unrestrictedItems.slice(n.renamed.position)]
                    itemsMap = createItemsMap(unrestrictedItems)
                }
            }
        })
        tableView.refresh()
        await delayAsync(40) 
    }
}

function delayAsync(ms) {
    return new Promise(res => setTimeout(res, ms))
}

async function deleteItems() {
    const items = getSelectedItems()
    if (items.length == 0)
        return
    const response = await fetch("request/deleteItems", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(items)
    })
    const res = await response.json()
    if (res.success)
        stopRestriction()
}

async function copy(move) {
    const items = getSelectedItems()
    if (items.length == 0)
        return
    const response = await fetch("request/copy", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            items,
            move 
        })
    })
    await response.json()
}

async function adaptPath() {
    const res = await fetch("request/adaptpath")
}

async function execute() {
    const res = await fetch(`request/execute/${tableView.getPosition()}`)
}

async function showProperties() {
    const res = await fetch(`request/showProperties/${tableView.getPosition()}`)
}

async function openWith() {
    const res = await fetch(`request/openWith/${tableView.getPosition()}`)
}

async function showFavorites() {
    stopRestriction()
    const response = await fetch("request/changePath?path=fav")
    const res = await response.json()
    if (res.itemsResult)
        setItems(res.itemsResult.items)
}

async function rename(asCopy) {
    const items = getSelectedItems()
    if (items.length != 1)
        return
    const res = await fetch("request/rename", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            item: items[0],
            asCopy
        })
    })
    console.log("res", res)
    stopRestriction()
}

/**
 * Rerieving selected items (as indexes)
 * @returns array of indexes
 */
function getSelectedItems() {
    const tableViewItems = tableView.getItems()
    const pos = unrestrictedItems
        ? itemsMap.get(tableViewItems[tableView.getPosition()].text).idx
        : tableView.getPosition()
    let selectedItems = unrestrictedItems ?
        tableViewItems
            .map(n => itemsMap.get(n.text)) 
            .map(n => n.item.selected ? n.idx : -1)
            .filter(n => n != -1)
        : tableViewItems
            .map((n, i) => n.selected ? i : -1)
            .filter(n => n != -1)
    return selectedItems = selectedItems.length > 0
        ? selectedItems
        : pos > 0 && (unrestrictedItems ? itemsMap.get(pos).isSelectable : tableViewItems[pos].isSelectable)
        ? [pos]
        : []
}

var itemsMap
var columnCount = 0
var unrestrictedItems
var changeDetectionCancellation = false