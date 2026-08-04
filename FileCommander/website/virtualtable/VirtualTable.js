import './Scrollbar.js'
import { ColumnsHeader } from "./ColumnsHeader.js"

// TODO scrollbar page up/down
// TODO Styling with color filters
// TODO Styling columns

export class VirtualTable extends HTMLElement {
    #offset = 0

    constructor() {
        super()
        this.itemHeight = 0
        this.currentPosition = 0
        this.visualItemsCount = 0
        this.items = []

        const style = document.createElement("style")
        document.head.appendChild(style)
        style.textContent = `:root {
            --vtc-current-color: lightgray;
            --vtc-current-focus-color: red;
            --vtc-font-size: 100%;
            --vtc-selected-background-color: blue;
            --vtc-scrollbar-width: 12px;

            --vtc-scrollbar-grip-width: 4px;
            --vtc-scrollbar-grip-radius: 999px;
            --vtc-scrollbar-grip-color: gray;
            --vtc-scrollbar-grip-active-color: var(--vtc-selected-background-color);
            --vtc-scrollbar-grip-right: 1px;
            
            --vtc-scrollbar-border-color: gray;
            --vtc-scrollbar-border-width: 1px;
            --vtc-scrollbar-background-color: white;
            --vtc-scrollbar-button-background-color: white;
            --vtc-scrollbar-button-color: #666;
            --vtc-scrollbar-button-hover-color: #555
            --vtc-scrollbar-button-active-color: #444
            --vtc-scrollbar-button-hover-background-color: rgb(209, 209, 209);
            --vtc-scrollbar-button-active-background-color: #aaa;
            --vtc-scrollbar-grip-color: rgb(209, 209, 209); 
            --vtc-scrollbar-grip-hover-color: #bbb;
            --vtc-scrollbar-right-margin: 15px;
            --vtc-caption-color: gray;
            --vtc-caption-background-color: #efefef;
            --vtc-caption-background-hover-color: lightgray;
            --vtc-caption-separator-color: white;
        }
        @media (prefers-color-scheme: dark) {
            :root {
                --vtc-caption-background-color: #212121;
            }
        }`
    }

    set offset(val) {
        if (this.#offset != val)
            this.scrollbar.scrollPosition = val
        this.#offset = val
    }

    get offset() {
        return this.#offset
    }

    connectedCallback() {
        this.shadow = this.attachShadow({ mode: "open" })
        this.root = document.createElement("div")
        this.root.id = "root"
        this.root.setAttribute("tabindex", "0")
        this.root.addEventListener("keydown", evt => this.onKeyDown(evt))
        this.root.addEventListener("wheel", evt => this.onWheel(evt))
        this.table = document.createElement("table")
        this.tableHead = document.createElement("thead")
        this.tableHeadRow = document.createElement("tr")
        this.columnsHeader = new ColumnsHeader(this.tableHeadRow, evt => this.onSort(evt), evt => this.onColumnWidthChange(evt))
        this.tableHead.appendChild(this.tableHeadRow)
        this.table.appendChild(this.tableHead)
        this.tableBody = document.createElement("tbody")
        this.tableBody.addEventListener("mousedown", evt => this.onMouseDown(evt))
        this.tableBody.addEventListener("dblclick", () => this.onSelected())
        this.table.appendChild(this.tableBody)
        this.root.appendChild(this.table)
        this.scrollbar = document.createElement("scroll-bar")
        this.scrollbar.addEventListener("scrollbar-scrolled", evt => this.onScrolled(evt))
        this.root.appendChild(this.scrollbar)
        this.shadow.appendChild(this.root)
        this.setAttribute("tabindex", "0")
        this.addEventListener("focus", () => this.root.focus())

        const resizeObserver = new ResizeObserver(() => this.onResize())
        resizeObserver.observe(this.root)

        const style = document.createElement('style')
        style.textContent = `
            #root {
                overflow: hidden;
                position: absolute;
                width: 100%;
                height: 100%;
                outline: none;
            }
            table {
                border-spacing: 0px;
                table-layout: fixed;
                width: 100%;        
            }
            td {
                padding-left: 6px;
                text-overflow: ellipsis;
                overflow: hidden;
                white-space: nowrap;
                -webkit-user-select: none;
                user-select: none;
                transition: padding-right .4s;
            }
            td:first-child {
                padding-left: 1px;
            }
            th.rightAligned {
                text-align: right;
            }
            td.rightAligned {
                text-align: right;
                padding-right: 5px;
            }
            tr.isCurrent {
                outline-color: var(--vtc-current-color);
                outline-width: 1px;
                outline-style: solid;
                outline-offset: -1px;    
            }
            .scrollbarActive tr td:last-child {
                padding-right: calc(3px + var(--vtc-scrollbar-right-margin));
            }                
            thead {
                color: var(--vtr-caption-color);
                background-color: var(--vtc-caption-background-color);
            }
            th {
                text-overflow: ellipsis;
                -webkit-user-select: none;
                user-select: none;
                text-align: left;
                font-weight: normal;
                border-left-style: solid;
                border-left-width: 1px;
                border-left-color: var(--vtc-caption-separator-color);
                padding-left: 5px;
                padding-right: 5px;
                overflow: hidden;
                white-space: nowrap;
            }
            th:first-child {
                border-left-width: 0px;
            }
            #root:has(#scrollbar:hover) tr td:last-child, #root:has(#grip:active) tr td:last-child {
                padding-right: calc(3px + var(--vtc-scrollbar-right-margin));
            }
            th.sortable, th.sortable span {
                background-color: transparent;
                transition: background-color 0.3s;
            }
            th.sortable:hover:not(:has(span:hover))  {
                background-color: var(--vtc-caption-background-hover-color);
            }
            th.sortable span:hover {
                background-color: var(--vtc-caption-background-hover-color);
            }
            .sortable .sortAscending:before, .sortable.sortAscending:before {
                position: relative;
                bottom: 11px;
                border-left: 4px solid transparent;
                border-right: 4px solid transparent;
                border-bottom: 6px solid var(--vtc-caption-color);
                content: '';
                margin-right: 5px;
            }
            .sortable .sortDescending:before, .sortable.sortDescending:before {
                position: relative;
                top: 10px;
                border-left: 4px solid transparent;
                border-right: 4px solid transparent;
                border-top: 6px solid var(--vtc-caption-color);
                content: '';
                margin-right: 5px;
            }
            th .subColumns {
                display: flex;
            }
            th .subColumnName {
                flex-grow: 1;
            }
            #root:focus tr.isCurrent {
                outline-color: var(--vtc-current-focus-color);
            }`

        this.shadow.appendChild(style)
    }

    setColumns(columns) {
        this.columnsHeader.setColumns(columns)
        this.scrollbar.setHeightOffset(this.tableHeadRow.clientHeight)
    }

    setColumnWidths(widths) {
        this.columnsHeader.setWidths(widths)
    }

    setOnSort(cb) {
        this.onSort = cb
    }

    setOnColumnWidthChange(cb) {
        this.onColumnWidthChange = cb
    }

    setItems(items, pos) {
        this.currentPosition = 0
        this.offset = 0
        this.items = items
        if (this.itemHeight == 0)
            this.measure()
        this.scrollbar.setCount(this.items.length)

        while (this.tableBody.lastElementChild)
            this.tableBody.removeChild(this.tableBody.lastElementChild)

        const count = this.getVisualItems()

        items
            .filter((_, idx) => idx <= count)
            .forEach((item, idx) => {
                const tr = this.createItem(item, idx)
                this.tableBody.appendChild(tr)
            })
    }

    setPosition(newPos) {
        const up = newPos < this.currentPosition
        newPos = up ? Math.max(newPos, 0) : Math.min(newPos, this.items.length - 1)
        const delta = this.scrollIntoView(newPos, up)
        const elements = Array.from(this.tableBody.children)
        const element = elements[this.currentPosition - this.offset]
        if (element)
            element.classList.remove("isCurrent")
        const newElement = elements[newPos - this.offset]
        newElement.classList.add("isCurrent")
        this.currentPosition = newPos
    }

    getPosition() {
        return this.currentPosition
    }

    async setStylesheet(cssUrl) {
        const res = await fetch(cssUrl)
        const cssText = await res.text()
        // Create style element
        const style = document.createElement('style')
        style.textContent = cssText
        // Append style to shadow root
        this.shadow.append(style)
    }

    measure() {
        var tr = this.createRowItem()
        this.measureRowItem(tr)
        this.tableBody.appendChild(tr)
        this.itemHeight = tr.clientHeight
        this.visualItemsCount = this.getVisualItems()
        this.scrollbar.setDisplayCount(this.visualItemsCount)
    }

    getVisualItems() {
        return Math.floor((this.root.clientHeight - this.tableHead.clientHeight) / this.itemHeight)
    }

    scrollToOffset() {
        const elements = Array.from(this.tableBody.children)
        elements.forEach((element, idx) => {
            this.checkCurrentItem(element, this.offset + idx)
            this.renderRowItem(element, this.items[this.offset + idx])
        })
    }

    createRowItem() {
        const event = new CustomEvent('create-rowitem', {
            bubbles: false,
            cancelable: false,
            detail: { tr: null }
        })
        this.dispatchEvent(event)
        return event.detail.tr
    }

    measureRowItem(tr) {
        const event = new CustomEvent('measure-rowitem', {
            bubbles: false,
            cancelable: false,
            detail: { tr }
        })
        this.dispatchEvent(event)
    }

    renderRowItem(tr, item) {
        if (item) {
            tr.style.setProperty('display', "")
            const event = new CustomEvent('render-rowitem', {
                bubbles: false,
                cancelable: false,
                detail: { tr, item }
            })
            this.dispatchEvent(event)
            const tds = Array.from(tr.children)
            tds.forEach((td, idx) => {
                if (this.columnsHeader.isRightAligned(idx))
                    td.classList.add("rightAligned")
                else
                    td.classList.remove("rightAligned")
            })
        }
        else
            tr.style.setProperty('display', 'none')
    }

    scroll(up) {
        if (!up) {
            if (this.offset + this.visualItemsCount >= this.items.length)
                return
            this.offset++
            const recycled = this.tableBody.firstElementChild
            recycled.remove()

            this.checkCurrentItem(recycled, this.offset + this.visualItemsCount)
            this.renderRowItem(recycled, this.items[this.offset + this.visualItemsCount])
            this.tableBody.appendChild(recycled)
        } else {
            if (this.offset <= 0)
                return
            this.offset--
            const recycled = this.tableBody.lastElementChild
            recycled.remove()
            this.checkCurrentItem(recycled, this.offset)
            this.renderRowItem(recycled, this.items[this.offset])
            this.tableBody.insertBefore(recycled, this.tableBody.firstElementChild)
        }
    }

    onResize() {
        if (this.items.length == 0)
            return
        const itemsCount = this.visualItemsCount
        this.visualItemsCount = this.getVisualItems()
        this.scrollbar.setDisplayCount(this.visualItemsCount)
        const elements = Array.from(this.tableBody.children)
        var tooMuch = elements.length - this.visualItemsCount - 1
        if (tooMuch > 0) {
            for (let i = 0; i < tooMuch; i++) {
                const recycled = this.tableBody.lastElementChild
                recycled.remove()
            }
        } else if (tooMuch < 0) {
            for (let i = 0; i < -tooMuch && itemsCount + i < this.items.length; i++) {
                const tr = this.createItem(this.items[itemsCount + i + 1 + this.offset], -1)
                this.tableBody.appendChild(tr)
            }
        }
        // TODO check if too small
        this.setPosition(this.currentPosition)
    }

    onKeyDown(evt) {
        if (evt.key == "ArrowDown") {
            evt.preventDefault()
            evt.stopPropagation()
            this.setPosition(this.currentPosition + 1)
        }
        else if (evt.key == "ArrowUp") {
            evt.preventDefault()
            evt.stopPropagation()
            this.setPosition(this.currentPosition - 1)
        }
        else if (evt.key == "PageDown") {
            evt.preventDefault()
            evt.stopPropagation()
            this.setPosition(this.currentPosition + this.visualItemsCount - 1)
        }
        else if (evt.key == "PageUp") {
            evt.preventDefault()
            evt.stopPropagation()
            this.setPosition(this.currentPosition - this.visualItemsCount + 1)
        }
        else if (evt.key == "End") {
            evt.preventDefault()
            evt.stopPropagation()
            this.offset = Math.max(this.items.length - this.visualItemsCount, 0)
            this.currentPosition = this.items.length - 1
            this.scrollToOffset()
        }
        else if (evt.key == "Home") {
            evt.preventDefault()
            evt.stopPropagation()
            this.offset = 0
            this.currentPosition = 0
            this.scrollToOffset()
        }
        else if (evt.key == "Enter") {
            evt.preventDefault()
            evt.stopPropagation()
            this.onSelected()
        }
    }

    onMouseDown(evt) {
        const rect = this.table.getBoundingClientRect()
        const y = evt.clientY - rect.top
        const index = Math.floor((y - this.tableHead.clientHeight) / this.itemHeight)
        const elements = Array.from(this.tableBody.children)
        let element = elements[this.currentPosition - this.offset]
        if (element)
            element.classList.remove("isCurrent")
        element = elements[index]
        if (element)
            element.classList.add("isCurrent")
        this.currentPosition = index + this.offset
    }

    onWheel(evt) {
        const delta = evt.deltaY / Math.abs(evt.deltaY) * 3
        if (this.items.length > this.visualItemsCount && !Number.isNaN(delta)) {
            this.scroll(delta < 0)
            this.scroll(delta < 0)
            this.scroll(delta < 0)
        }
    }

    onScrolled(evt) {
        const scroll = this.offset != evt.detail.pos
        this.offset = evt.detail.pos
        if (scroll)
            this.scrollToOffset()
    }

    onSelected() {
        if (this.currentPosition == Infinity || this.currentPosition < 0)
            return
        const event = new CustomEvent('process-selected', {
            bubbles: false,
            cancelable: false,
            detail: { pos: this.currentPosition }
        })
        this.dispatchEvent(event)
    }

    scrollIntoView(newPos, up) {

        const scrollDown = () => {
            const offset = newPos - this.offset - this.visualItemsCount + 1
            if (offset >= 0) {
                const elements = Array.from(this.tableBody.children)
                for (let i = 0; i < offset; i++) {
                    const recycled = this.tableBody.firstElementChild
                    recycled.remove()
                    recycled.classList.remove("isCurrent")
                    this.renderRowItem(recycled, this.items[this.offset + this.visualItemsCount + 1 + i])
                    this.tableBody.appendChild(recycled)
                }

                this.offset += offset
                return offset
            }
            return 0
        }

        const scrollUp = () => {
            const offset = newPos - this.offset
            if (offset < 0) {
                const elements = Array.from(this.tableBody.children)
                if (newPos >= 0) {
                    for (let i = 0; i < -offset; i++) {
                        const recycled = this.tableBody.lastElementChild
                        recycled.remove()
                        recycled.classList.remove("isCurrent")
                        this.renderRowItem(recycled, this.items[this.offset - 1 - i])
                        this.tableBody.insertBefore(recycled, this.tableBody.firstElementChild)
                    }
                }
                this.offset += offset
                return offset
            }
            return 0
        }

        if (!up) {
            const res = scrollDown()
            if (res != 0)
                return res
        } else {
            const res = scrollUp()
            if (res != 0)
                return res
        }
        if (!up && this.currentPosition < this.offset)
            return scrollUp()
        else if (this.currentPosition > this.offset + this.visualItemsCount)
            return scrollDown()
        return 0
    }

    checkCurrentItem(element, idx) {
        if (idx == this.currentPosition)
            element.classList.add("isCurrent")
        else
            element.classList.remove("isCurrent")
    }

    createItem(item, idx) {
        const tr = this.createRowItem()
        this.checkCurrentItem(tr, idx)
        this.renderRowItem(tr, item)
        return tr
    }
}

customElements.define("virtual-table", VirtualTable)