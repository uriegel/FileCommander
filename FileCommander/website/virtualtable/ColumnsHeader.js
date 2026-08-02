export class ColumnsHeader
{
    constructor(tableHeadRow)
    {
        this.tableHeadRow = tableHeadRow
        this.tableHeadRow.addEventListener("mousemove", evt => this.onMouseMove(evt))
        this.tableHeadRow.addEventListener("mousedown", evt => this.onMouseDown(evt))
        this.sortIndex = -1
        this.sortDescending = true // to initial turn to false
    }

    setColumns(columns)
    {
        this.columns = columns
        while (this.tableHeadRow.lastElementChild)
            this.tableHeadRow.removeChild(this.tableHeadRow.lastElementChild)
        columns.forEach((item, idx) => {
            const th = document.createElement("th")
            th.onclick = evt => this.onColumnClick(idx, evt)
            if (item.subColumn)
            {
                const div = document.createElement("div")
                div.classList.add("subColumns")
                const col = document.createElement("span")
                col.textContent = item.text
                col.classList.add("subColumnName")
                div.appendChild(col)
                const subcol = document.createElement("span")
                subcol.textContent = item.subColumn
                subcol.classList.add("subColumn")
                div.appendChild(subcol)
                th.appendChild(div)
            }
            else
                th.textContent = item.text
            if (item.isRightAligned)
                th.classList.add("rightAligned")
            else
                th.classList.remove("rightAligned")
            if (item.sort)
                th.classList.add("sortable")
            this.tableHeadRow.appendChild(th)
        })
    }

    isRightAligned(idx) { return this.columns[idx].isRightAligned }

    onMouseMove(evt)
    {
        const element = evt.target.tagName == "TH" ? evt.target : evt.target.parentElement?.parentElement
        const thWidth = element.clientWidth + element.clientLeft
        const mouseX = evt.offsetX + element.clientLeft
        const trRect = element.parentElement?.getBoundingClientRect()
        const absoluteRight = trRect.width + trRect.x
        let dr =
            (mouseX < 3 || mouseX > thWidth - 4)
            && (evt.pageX - trRect.x > 4)
            && (evt.pageX < absoluteRight - 4)
        if (dr && evt.target.tagName != "TH")
        {
            const first = evt.target.style.flexGrow == "1"
            if (first && mouseX > thWidth - 4 || !first && mouseX < 3)
                dr = false
        }
        this.draggingReady = dr
        document.body.style.cursor = dr ? 'ew-resize' : 'auto'
    }

    onMouseDown(evt)
    {
        if (!this.draggingReady)
            return
        this.dragging = true
        const th = evt.target
        const mouseX = evt.offsetX + th.clientLeft
        const dragleft = mouseX < 3

        const startDragPosition = evt.pageX
        const targetColumn = th.closest("th")

        const currentHeader = dragleft ? targetColumn?.previousElementSibling : targetColumn
        if (!currentHeader)
            return
        const nextHeader = currentHeader.nextElementSibling
        if (!nextHeader)
            return

        const currentLeftWidth = currentHeader?.offsetWidth
        const sumWidth = currentLeftWidth + nextHeader?.offsetWidth

        const onmove = (evt) => {
            document.body.style.cursor = 'ew-resize'
            let diff = evt.pageX - startDragPosition
            if (currentLeftWidth + diff < 15)
                diff = 15 - currentLeftWidth
            else if (diff > sumWidth - currentLeftWidth - 15)
                diff = sumWidth - currentLeftWidth - 15

            const getCombinedWidth = (column, nextColumn) => {
                const firstWidth =
                    column.style.width
                    ? parseFloat(column.style.width.substring(0, column.style.width.length - 1))
                    : 100 / this.columns.length
                const secondWidth =
                    nextColumn.style.width
                    ? parseFloat(nextColumn.style.width.substring(0, nextColumn.style.width.length - 1))
                    : 100 / this.columns.length
                return firstWidth + secondWidth
            }

            const combinedWidth = getCombinedWidth(currentHeader, nextHeader)

            let leftWidth = currentLeftWidth + diff
            let rightWidth = sumWidth - currentLeftWidth - diff
            const factor = combinedWidth / sumWidth
            leftWidth = leftWidth * factor
            rightWidth = rightWidth * factor

            currentHeader.style.width = leftWidth + '%'
            nextHeader.style.width = rightWidth + '%'
            evt.preventDefault()
        }

        const onup = (evt) => {

            const preventClickOnResetting = () => setTimeout(() => this.dragging = false)


            const getWidths = () => {
                const ths = Array.from(this.tableHeadRow.children)
                return ths.map(th =>
                    th.style.width
                        ? parseFloat(th.style.width.substring(0, th.style.width.length - 1))
                        : 100 / columns.length
                )
            }

            window.removeEventListener('mousemove', onmove)
            window.removeEventListener('mouseup', onup)
            document.body.style.cursor = 'auto'
            //setColumnWidths(getWidths())
            preventClickOnResetting()
            evt.preventDefault()
            evt.stopPropagation()
        }

        window.addEventListener('mousemove', onmove)
        window.addEventListener('mouseup', onup)
        evt.preventDefault()
        evt.stopPropagation()
    }

    onColumnClick(idx, evt)
    {
        if (this.dragging)
            return
        if (this.columns[idx].sort)
        {
            const ths = Array.from(this.tableHeadRow.children)
            if (this.sortIndex != -1)
            {
                if (!this.columns[this.sortIndex].subColumn)
                    ths[this.sortIndex].classList.remove(this.sortDescending ? "sortDescending" : "sortAscending")
                else
                {
                    if (this.subColumn)
                        ths[this.sortIndex].firstChild.lastElementChild.classList.remove(this.sortDescending ? "sortDescending" : "sortAscending")
                    else
                        ths[this.sortIndex].firstChild.firstChild.classList.remove(this.sortDescending ? "sortDescending" : "sortAscending")
                }
            }
            this.sortDescending = !this.sortDescending
            if (!this.columns[idx].subColumn)
                ths[idx].classList.add(this.sortDescending ? "sortDescending" : "sortAscending")
            else
            {
                if (evt.originalTarget.classList.contains("subColumn"))
                {
                    this.subColumn = true
                    evt.originalTarget.classList.add(this.sortDescending ? "sortDescending" : "sortAscending")
                }
                else
                {
                    this.subColumn = false
                    evt.originalTarget.classList.add(this.sortDescending ? "sortDescending" : "sortAscending")
                }
            }
            this.columns[idx].sort({
            subColumn: this.subColumn || undefined,
                descending: this.sortDescending
            })
            this.sortIndex = idx
        }
    }
}