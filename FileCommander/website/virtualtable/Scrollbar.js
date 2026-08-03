const minScrollbarGripSize = 20

export class Scrollbar extends HTMLElement {
    #scrollbarGripTop = 0
    #scrollPosition = 0

    constructor() {
        super()
        this.count = 0
        this.displayCount = 0
        this.range = 1
        this.gripHeight = 0
        this.scrollPosition = 0
    }

    connectedCallback() {
        this.id = "scrollbar"
        this.grip = document.createElement("div")
        this.grip.id = "grip"
        this.addEventListener("mousedown", evt => this.onPageMouseDown(evt))
        this.grip.addEventListener("mousedown", evt => this.onGripMouseDown(evt))
        this.appendChild(this.grip)

        const style = document.createElement('style')
        style.textContent = `
            #scrollbar {
                width: var(--vtc-scrollbar-width);
                right: 0px;
                position: absolute;
                overflow: hidden;
                background-color: transparent;
                outline-width: 0px;
                outline-style: none;    
                -webkit-user-select: none;
                user-select: none;
                display: flex;
                flex-direction: column;
                transition: transform 0.3s, Opacity 0.3s;
                transform-origin: right top;
                bottom: 0px;    
            }
            #scrollbar.hidden {
                transform: scale(0);
                opacity: 0;            
            }
            #grip {
                position: absolute;
                border-radius: var(--vtc-scrollbar-grip-radius);
                background-color: var(--vtc-scrollbar-grip-color);
                width: var(--vtc-scrollbar-grip-width);
                right: var(--vtc-scrollbar-grip-right);
                transition: background-color 0.5s, width 0.5s;    
            }
            #grip:active {
                background-color: var(--vtc-scrollbar-grip-active-color);
                width: calc(100% - var(--vtc-scrollbar-grip-right));
                transition: background-color 0s;
            }
            #scrollbar:hover #grip {
                width: calc(100% - var(--vtc-scrollbar-grip-right));
            }`

        this.appendChild(style)
    }

    get scrollbarGripTop() { return this.#scrollbarGripTop }
    set scrollbarGripTop(val) {
        this.#scrollbarGripTop = val
        this.grip?.style.setProperty('top', `${val}px`)
    }

    get scrollPosition() { return this.#scrollPosition }
    set scrollPosition(val) {
        this.#scrollPosition = val
        this.scrollbarGripTop = this.getScrollbarGripTop()
    }

    setHeightOffset(headerHeight) {
        this.style.setProperty('height', `calc(100% - ${headerHeight}px)`);
    }

    onPageMouseDown(evt) {
        evt.preventDefault()
        evt.stopPropagation()
        const action = () => {
            let newPosition = this.#scrollPosition
            if (evt.offsetY <= this.scrollbarGripTop) {
                if (evt.offsetY < this.scrollbarGripTop)
                    newPosition -= this.displayCount
                this.scrollPosition = Math.max(newPosition, 0)
                this.emitScrollPosition(this.scrollPosition)
            } else {
                if (evt.offsetY > this.scrollbarGripTop + this.gripHeight)
                    newPosition += this.displayCount
                this.scrollPosition = Math.min(this.range - 1, newPosition)
                this.emitScrollPosition(this.scrollPosition)
            }
        }

        const mouseRepeat = action => {
            action()
            let interval = 0
            const timeout = setTimeout(() => interval = setInterval(() => action(), 50), 600)
            const mouseUp = () => {
                window.removeEventListener("mouseup", mouseUp)
                clearTimeout(timeout)
                if (interval)
                    clearInterval(interval)
            }
            window.addEventListener("mouseup", mouseUp)
        }

        mouseRepeat(() => action())
    }

    onGripMouseDown(evt) {
        const pixelRange = this.offsetHeight - this.gripHeight + 1
        const maxPosition = this.count - this.displayCount
        const startPos = evt.y - this.scrollbarGripTop

        const onmove = (evt) => {
            const delta = evt.y - startPos
            if (pixelRange) {
                const factor = Math.min(1, (Math.max(0, delta * 1.0 / pixelRange)))
                this.emitScrollPosition(Math.floor(factor * maxPosition))
            }
            evt.preventDefault()
            evt.stopPropagation()
        }
        const onup = () => {
            window.removeEventListener('mousemove', onmove, true)
            window.removeEventListener('mouseup', onup, true)
        }
        window.addEventListener('mousemove', onmove, true)
        window.addEventListener('mouseup', onup, true)

        evt.stopPropagation()
    }

    emitScrollPosition(pos) {
        const event = new CustomEvent('scrollbar-scrolled', {
            bubbles: false,
            cancelable: false,
            detail: { pos: pos }
        })
        this.dispatchEvent(event)
    }

    setCount(val) {
        this.count = val
        this.range = this.setRange()
        this.gripHeight = this.getGripHeight()
        this.scrollbarGripTop = this.getScrollbarGripTop()
    }

    setDisplayCount(val) {
        this.displayCount = val
        this.range = this.setRange()
        this.gripHeight = this.getGripHeight()
        this.scrollbarGripTop = this.getScrollbarGripTop()
    }

    setRange() {
        const range = Math.max(0, this.count - this.displayCount)
        console.log("range", range)
        if (range > 0)
            this.classList.remove('hidden')
        else
            this.classList.add('hidden')
        return range
    }

    getGripHeight() {
        const height = Math.max(this.offsetHeight * (this.displayCount / this.count || 1), minScrollbarGripSize)
        this.grip.style.setProperty('height', `${height}px`)
        return height
    }
    getScrollbarGripTop() {
        return (this.offsetHeight - this.gripHeight) * (this.scrollPosition / this.range)
    }
}

customElements.define("scroll-bar", Scrollbar)
