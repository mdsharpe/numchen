<template>
  <div class="game">
    <div class="header">
      <div class="header-left">
        <span class="join-code">{{ joinCode }}</span>
        <span class="player-list">{{ players.join(", ") }}</span>
      </div>
      <div class="header-right">
        <button
          v-if="!gameStarted"
          class="btn primary"
          @click="startGame"
        >
          Start Game
        </button>
        <button
          v-if="!gameFinished"
          class="btn"
          :class="{ active: autoPlay }"
          @click="toggleAutoPlay"
        >
          {{ autoPlay ? "Auto Play: ON" : "Auto Play" }}
        </button>
      </div>
    </div>

    <div class="top-row">
      <div class="draw-area">
        <div v-if="currentCard !== null && !hasPlaced" class="draw-pile">
          <div
            class="drawn-card"
            :class="{ 'drag-source': drag?.type === 'drawn' && drag.isDragging, 'tilted': hoverColumn !== null && currentCard !== null && !hasPlaced }"
            @pointerdown="onDrawnPointerDown"
          >
            <span class="card-pip top-left">{{ currentCard }}</span>
            <span class="card-value">{{ currentCard }}</span>
            <span class="card-pip bottom-right">{{ currentCard }}</span>
          </div>
          <div class="draw-label">Place this card</div>
          <div v-if="countdown !== null" class="timer-bar-container">
            <div
              class="timer-bar"
              :class="{ urgent: countdown <= 5 }"
              :style="{ width: timerPercent + '%' }"
            ></div>
          </div>
        </div>
        <div v-else-if="hasPlaced" class="draw-pile">
          <div class="drawn-card placeholder"></div>
          <div class="draw-label">Waiting for other players...</div>
          <div v-if="countdown !== null" class="timer-bar-container">
            <div
              class="timer-bar"
              :style="{ width: timerPercent + '%' }"
            ></div>
          </div>
        </div>
        <div v-else class="draw-pile">
          <div class="drawn-card placeholder"></div>
        </div>
      </div>

      <div class="piles" :class="{ 'drag-over': dragOverDestinations }" data-drop="destinations">
        <div v-for="(pile, index) in destinations" :key="index" class="pile" data-drop="destinations">
          <div class="dest-card" :class="{ empty: pile === 0 }" data-drop="destinations">
            {{ pile > 0 ? pile : "" }}
          </div>
        </div>
      </div>
    </div>

    <div class="tableau">
      <div
        v-for="(column, index) in columns"
        :key="index"
        class="column"
        :class="{
          'drop-target': currentCard !== null && !hasPlaced && !isProcessing,
          'drag-over': dragOverColumn === index,
        }"
        :data-column-index="index"
        @click="onPlaceCard(index)"
        @pointerenter="hoverColumn = index"
        @pointerleave="hoverColumn = hoverColumn === index ? null : hoverColumn"
      >
        <div class="card-stack">
          <div v-if="column.length === 0" class="card empty-placeholder"></div>
          <div
            v-for="(card, cardIndex) in column"
            :key="cardIndex"
            class="card"
            :class="{
              'top-card': cardIndex === column.length - 1 && !isProcessing && !gameFinished && canMoveToDestination(card),
              'drag-source': drag?.type === 'column' && drag.columnIndex === index && drag.isDragging && cardIndex === column.length - 1,
            }"
            :style="{ top: cardIndex * getCardOffset(column.length) + 'px', zIndex: cardIndex }"
            :data-column-index="index"
            @click="onCardClick($event, index, cardIndex)"
            @pointerdown="cardIndex === column.length - 1 && !gameFinished && canMoveToDestination(card) && onColumnCardPointerDown($event, index, card)"
          >
            <span class="card-pip top-left">{{ card }}</span>
            <span class="card-value">{{ card }}</span>
            <span class="card-pip bottom-right">{{ card }}</span>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="drag && drag.isDragging"
      class="drag-card"
      :style="{ left: drag.x + 'px', top: drag.y + 'px' }"
    >
      <span class="card-pip top-left">{{ drag.cardValue }}</span>
      <span class="card-value">{{ drag.cardValue }}</span>
      <span class="card-pip bottom-right">{{ drag.cardValue }}</span>
    </div>

    <div v-if="gameFinished" class="finished-overlay">
      <div class="finished-panel">
        <div class="finished-icon">&#10003;</div>
        <div class="finished-text">Game Over!</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getGameHub } from "@/gameHub";

const route = useRoute();
const router = useRouter();
const hub = getGameHub();

const PLACEMENT_TIMEOUT_SECONDS = 30;

const joinCode = route.params.joinCode as string;
const players = ref<string[]>([]);
const gameStarted = ref(false);
const gameFinished = ref(false);
const currentCard = ref<number | null>(null);
const hasPlaced = ref(false);
const columns = ref<number[][]>([[], [], [], [], [], []]);
const destinations = ref<number[]>([0, 0, 0, 0, 0, 0]);
const autoPlay = ref(false);
const autoPlayDelay = 400;
const isProcessing = ref(false);
let autoPlayGeneration = 0;
const countdown = ref<number | null>(null);
let countdownInterval: ReturnType<typeof setInterval> | null = null;
const windowWidth = ref(window.innerWidth);

function onWindowResize() {
  windowWidth.value = window.innerWidth;
}

const timerPercent = computed(() => {
  if (countdown.value === null) {
    return 0;
  }
  return Math.max(0, (countdown.value / PLACEMENT_TIMEOUT_SECONDS) * 100);
});

function startCountdown(deadline: number | null) {
  stopCountdown();
  if (deadline === null) {
    return;
  }

  const update = () => {
    const remaining = Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
    countdown.value = remaining;
    if (remaining <= 0) {
      stopCountdown();
    }
  };

  update();
  countdownInterval = setInterval(update, 250);
}

function stopCountdown() {
  countdown.value = null;
  if (countdownInterval !== null) {
    clearInterval(countdownInterval);
    countdownInterval = null;
  }
}

function onPlayerJoined(playerName: string) {
  players.value.push(playerName);
}

function onPlayerLeft(playerName: string) {
  players.value = players.value.filter((p) => p !== playerName);
}

function cancelDrag() {
  if (drag.value) {
    document.removeEventListener("pointermove", onPointerMove);
    document.removeEventListener("pointerup", onPointerUp);
    drag.value = null;
    dragOverColumn.value = null;
    dragOverDestinations.value = false;
  }
}

function onCardAutoPlaced(columnIndex: number) {
  if (currentCard.value === null || hasPlaced.value) {
    return;
  }

  cancelDrag();
  columns.value[columnIndex]!.push(currentCard.value);
  hasPlaced.value = true;
  currentCard.value = null;
}

function onCardDrawn(cardValue: number, deadline: number | null) {
  gameStarted.value = true;
  currentCard.value = cardValue;
  hasPlaced.value = false;
  autoPlayGeneration++;
  startCountdown(deadline);

  if (autoPlay.value) {
    scheduleAutoPlay(autoPlayGeneration);
  }
}

function onGameFinished() {
  gameFinished.value = true;
  currentCard.value = null;
  stopCountdown();
  sessionStorage.removeItem("numchen_session");
}

function getSavedSession(): { joinCode: string; playerId: string } | null {
  try {
    const raw = sessionStorage.getItem("numchen_session");
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw);
    if (parsed.joinCode === joinCode && parsed.playerId) {
      return parsed;
    }
  } catch {
    // Ignore invalid data
  }
  return null;
}

async function tryRejoin(): Promise<boolean> {
  const saved = getSavedSession();
  if (!saved) {
    return false;
  }

  try {
    await hub.start();
    const result = await hub.rejoinGame(saved.playerId);
    players.value = result.players;
    gameStarted.value = result.gameStarted;
    gameFinished.value = result.gameFinished;
    currentCard.value = result.currentCard;
    hasPlaced.value = result.hasPlaced;
    columns.value = result.columns;
    destinations.value = result.destinations;
    startCountdown(result.placementDeadline);
    return true;
  } catch {
    sessionStorage.removeItem("numchen_session");
    return false;
  }
}

onMounted(async () => {
  window.addEventListener("resize", onWindowResize);
  hub.on("PlayerJoined", onPlayerJoined);
  hub.on("PlayerLeft", onPlayerLeft);
  hub.on("CardAutoPlaced", onCardAutoPlaced);
  hub.on("CardDrawn", onCardDrawn);
  hub.on("GameFinished", onGameFinished);

  const rejoined = await tryRejoin();
  if (!rejoined) {
    router.replace({ name: "lobby" });
  }
});

onUnmounted(() => {
  window.removeEventListener("resize", onWindowResize);
  hub.off("PlayerJoined", onPlayerJoined);
  hub.off("PlayerLeft", onPlayerLeft);
  hub.off("CardAutoPlaced", onCardAutoPlaced);
  hub.off("CardDrawn", onCardDrawn);
  hub.off("GameFinished", onGameFinished);
  stopCountdown();
  document.removeEventListener("pointermove", onPointerMove);
  document.removeEventListener("pointerup", onPointerUp);
});

// Drag and drop
interface DragState {
  type: "drawn" | "column";
  columnIndex: number;
  cardValue: number;
  startX: number;
  startY: number;
  x: number;
  y: number;
  isDragging: boolean;
}

const hoverColumn = ref<number | null>(null);
const drag = ref<DragState | null>(null);
const dragOverColumn = ref<number | null>(null);
const dragOverDestinations = ref(false);
const DRAG_THRESHOLD = 5;

function onDrawnPointerDown(e: PointerEvent) {
  if (currentCard.value === null || hasPlaced.value || isProcessing.value) {
    return;
  }
  startDrag(e, "drawn", -1, currentCard.value);
}

function onColumnCardPointerDown(e: PointerEvent, columnIndex: number, cardValue: number) {
  if (isProcessing.value) {
    return;
  }
  startDrag(e, "column", columnIndex, cardValue);
}

function startDrag(e: PointerEvent, type: "drawn" | "column", columnIndex: number, cardValue: number) {
  e.preventDefault();
  drag.value = {
    type,
    columnIndex,
    cardValue,
    startX: e.clientX,
    startY: e.clientY,
    x: e.clientX,
    y: e.clientY,
    isDragging: false,
  };
  document.addEventListener("pointermove", onPointerMove);
  document.addEventListener("pointerup", onPointerUp);
}

function onPointerMove(e: PointerEvent) {
  if (!drag.value) {
    return;
  }
  const dx = e.clientX - drag.value.startX;
  const dy = e.clientY - drag.value.startY;
  if (!drag.value.isDragging && Math.sqrt(dx * dx + dy * dy) >= DRAG_THRESHOLD) {
    drag.value.isDragging = true;
  }
  if (drag.value.isDragging) {
    drag.value.x = e.clientX;
    drag.value.y = e.clientY;
    updateDragOver(e);
  }
}

function updateDragOver(e: PointerEvent) {
  const el = document.elementFromPoint(e.clientX, e.clientY) as HTMLElement | null;
  if (!el) {
    dragOverColumn.value = null;
    dragOverDestinations.value = false;
    return;
  }
  const colEl = el.closest("[data-column-index]");
  dragOverColumn.value = colEl ? parseInt(colEl.getAttribute("data-column-index")!) : null;
  dragOverDestinations.value = !!el.closest("[data-drop='destinations']");
}

function onPointerUp(e: PointerEvent) {
  document.removeEventListener("pointermove", onPointerMove);
  document.removeEventListener("pointerup", onPointerUp);

  if (!drag.value) {
    return;
  }

  const wasDragging = drag.value.isDragging;
  const dragState = drag.value;
  drag.value = null;
  dragOverColumn.value = null;
  dragOverDestinations.value = false;

  if (wasDragging) {
    suppressNextClick = true;
  }

  if (!wasDragging) {
    // Treat as click
    if (dragState.type === "drawn") {
      // No-op: let the column click handle placement
    } else if (dragState.type === "column") {
      onTopCardClick(dragState.columnIndex);
    }
    return;
  }

  // Find drop target
  const el = document.elementFromPoint(e.clientX, e.clientY);
  if (!el) {
    return;
  }

  if (dragState.type === "drawn") {
    const columnEl = (el as HTMLElement).closest("[data-column-index]");
    if (columnEl) {
      const colIndex = parseInt(columnEl.getAttribute("data-column-index")!);
      onPlaceCard(colIndex);
    }
  } else if (dragState.type === "column") {
    const destEl = (el as HTMLElement).closest("[data-drop='destinations']");
    if (destEl) {
      onTopCardClick(dragState.columnIndex);
    }
  }
}

const MAX_CARD_OFFSET = 18;
const MIN_CARD_OFFSET = 6;

function getCardOffset(cardCount: number): number {
  if (cardCount <= 1) {
    return 0;
  }
  const mobile = windowWidth.value < 640;
  const cardHeight = mobile ? 28 : 40;
  const maxStackHeight = mobile ? 200 : 280;
  const offset = (maxStackHeight - cardHeight) / (cardCount - 1);
  return Math.max(MIN_CARD_OFFSET, Math.min(MAX_CARD_OFFSET, offset));
}

function canMoveToDestination(cardValue: number): boolean {
  if (cardValue === 1) {
    return destinations.value.some((d) => d === 0);
  }
  return destinations.value.some((d) => d === cardValue - 1);
}

function findMovableColumns(): number[] {
  const movable: number[] = [];
  for (let i = 0; i < 6; i++) {
    const col = columns.value[i]!;
    if (col.length > 0 && canMoveToDestination(col[col.length - 1]!)) {
      movable.push(i);
    }
  }
  return movable;
}

function chooseColumn(cardValue: number): number {
  // If the card can go to a destination immediately, use the column with the fewest cards
  if (canMoveToDestination(cardValue)) {
    return columns.value
      .map((col, i) => ({ i, len: col.length }))
      .sort((a, b) => a.len - b.len)[0]!.i;
  }

  // Prefer empty columns
  const empty = columns.value
    .map((col, i) => ({ i, len: col.length }))
    .filter((c) => c.len === 0);
  if (empty.length > 0) {
    return empty[0]!.i;
  }

  // Place on the column whose top card is closest to (but >=) the new card value,
  // to keep lower cards near the top. Tie-break by fewest cards.
  const scored = columns.value
    .map((col, i) => {
      const top = col[col.length - 1]!;
      return { i, top, len: col.length };
    })
    .sort((a, b) => {
      // Prefer columns where top >= cardValue (card goes underneath conceptually)
      const aAbove = a.top >= cardValue ? 0 : 1;
      const bAbove = b.top >= cardValue ? 0 : 1;
      if (aAbove !== bAbove) {
        return aAbove - bAbove;
      }
      // Then prefer fewer cards
      return a.len - b.len;
    });

  return scored[0]!.i;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function getIsAutoPlayStale(generation: number): boolean {
  return !autoPlay.value || generation !== autoPlayGeneration;
}

async function autoMoveToDestinations(generation: number): Promise<void> {
  let moved = true;
  while (moved) {
    moved = false;
    const movable = findMovableColumns();
    for (const colIndex of movable) {
      if (getIsAutoPlayStale(generation)) {
        return;
      }
      await sleep(autoPlayDelay);
      if (getIsAutoPlayStale(generation)) {
        return;
      }
      await onTopCardClick(colIndex);
      moved = true;
      break; // Re-check after each move since state changed
    }
  }
}

async function scheduleAutoPlay(generation: number): Promise<void> {
  await sleep(autoPlayDelay);
  if (getIsAutoPlayStale(generation) || currentCard.value === null || hasPlaced.value) {
    return;
  }

  // Move any cards to destinations first
  await autoMoveToDestinations(generation);

  if (getIsAutoPlayStale(generation) || currentCard.value === null || hasPlaced.value) {
    return;
  }

  // Place the card
  const col = chooseColumn(currentCard.value);
  await onPlaceCard(col);

  // Move cards to destinations after placing
  await sleep(autoPlayDelay);
  if (getIsAutoPlayStale(generation)) {
    return;
  }
  await autoMoveToDestinations(generation);
}

async function toggleAutoPlay(): Promise<void> {
  autoPlay.value = !autoPlay.value;

  if (autoPlay.value) {
    if (!gameStarted.value) {
      await startGame();
    } else if (currentCard.value !== null && !hasPlaced.value) {
      scheduleAutoPlay(autoPlayGeneration);
    }
  }
}

async function startGame() {
  await hub.startGame();
}

async function onPlaceCard(index: number) {
  if (currentCard.value === null || hasPlaced.value || isProcessing.value) {
    return;
  }

  isProcessing.value = true;
  try {
    const card = currentCard.value;
    columns.value[index]!.push(card);
    hasPlaced.value = true;
    currentCard.value = null;
    await hub.placeCard(index);
  } finally {
    isProcessing.value = false;
  }
}

let suppressNextClick = false;

function onCardClick(event: Event, columnIndex: number, cardIndex: number) {
  if (suppressNextClick) {
    suppressNextClick = false;
    event.stopPropagation();
    return;
  }

  const column = columns.value[columnIndex]!;
  const isTopCard = cardIndex === column.length - 1;
  const topCard = isTopCard ? column[cardIndex]! : null;

  if (isTopCard && topCard !== null && !isProcessing.value && canMoveToDestination(topCard)) {
    event.stopPropagation();
    onTopCardClick(columnIndex);
  }
}

async function onTopCardClick(index: number) {
  const column = columns.value[index]!;
  if (column.length === 0 || isProcessing.value || gameFinished.value) {
    return;
  }

  isProcessing.value = true;
  try {
    const { pileIndex } = await hub.moveToDestination(index);
    const card = column.pop()!;
    destinations.value[pileIndex] = card;
  } catch {
    // Move wasn't valid, ignore
  } finally {
    isProcessing.value = false;
  }
}
</script>

<style scoped>
.game {
  --card-width: 70px;
  --card-height: 96px;
  --card-radius: 8px;

  display: flex;
  flex-direction: column;
  height: 100vh;
  height: 100dvh;
  padding: 0.25rem 0.5rem;
  position: relative;
  touch-action: manipulation;
}

/* Header */
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.25rem 0;
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.header-right {
  display: flex;
  gap: 0.5rem;
}

.join-code {
  background: var(--color-background-mute);
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.3rem 0.7rem;
  font-weight: 700;
  font-size: 1rem;
  letter-spacing: 0.1em;
}

.player-list {
  font-size: 1rem;
  opacity: 0.7;
}

.btn {
  padding: 0.5rem 1.2rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
}

.btn:hover {
  border-color: var(--color-border-hover);
  background: var(--color-background-mute);
}

.btn.primary {
  background: #2563eb;
  border-color: #2563eb;
  color: white;
}

.btn.primary:hover {
  background: #1d4ed8;
}

.btn.active {
  background: #16a34a;
  border-color: #16a34a;
  color: white;
}

.btn.active:hover {
  background: #15803d;
}

/* Top row — draw pile left, destinations right */
.top-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-shrink: 0;
  padding: 0.5rem 0;
}

.draw-area {
  flex-shrink: 0;
}

.draw-pile {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
}

.drawn-card {
  position: relative;
  width: var(--card-width);
  height: var(--card-height);
  border: 2px solid #2563eb;
  border-radius: var(--card-radius);
  background: var(--color-background);
  color: #2563eb;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.2);
  touch-action: none;
  cursor: grab;
  transition: transform 0.25s;
}

.drawn-card.tilted {
  transform: translate(2px, 4px) rotate(3deg);
}

.drawn-card.placeholder {
  border-style: dashed;
  border-color: var(--color-border);
  box-shadow: none;
  opacity: 0.3;
}

.draw-label {
  font-size: 1rem;
  opacity: 0.6;
  font-weight: 500;
}

.timer-bar-container {
  width: 140px;
  height: 5px;
  background: var(--color-background-mute);
  border-radius: 2px;
  overflow: hidden;
}

.timer-bar {
  height: 100%;
  background: #2563eb;
  border-radius: 2px;
  transition: width 0.25s linear;
}

.timer-bar.urgent {
  background: #dc2626;
}

/* Tableau */
.tableau {
  display: flex;
  gap: 0.5rem;
  flex: 1;
  min-height: 0;
}

.column {
  flex: 1;
  text-align: center;
  display: flex;
  flex-direction: column;
}

.card-stack {
  position: relative;
  flex: 1;
  min-height: 44px;
}

.card {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  width: var(--card-width);
  height: var(--card-height);
  border: 1px solid var(--color-border);
  border-radius: var(--card-radius);
  background: var(--color-background);
  color: var(--color-text);
  user-select: none;
}

.card-value {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  font-weight: 700;
}

.card-pip {
  position: absolute;
  font-size: 0.7rem;
  font-weight: 700;
  line-height: 1;
}

.card-pip.top-left {
  top: 4px;
  left: 6px;
}

.card-pip.bottom-right {
  bottom: 4px;
  right: 6px;
  transform: rotate(180deg);
}

.card.empty-placeholder {
  position: relative;
  border-style: dashed;
  opacity: 0.3;
  background: var(--color-background-mute);
}

.card.top-card {
  cursor: grab;
  touch-action: none;
  border-color: #16a34a;
  color: #16a34a;
  box-shadow: 0 0 0 1px rgba(22, 163, 74, 0.15);
  transition: all 0.15s;
}

.card.top-card:hover {
  background: var(--color-accent-green-hover);
  box-shadow: 0 0 0 2px rgba(22, 163, 74, 0.25);
}

.column.drop-target {
  cursor: pointer;
}

.column.drop-target:hover:not(:has(.top-card:hover)) {
  outline: 2px dashed #2563eb;
  outline-offset: -2px;
  border-radius: var(--card-radius);
  background: rgba(37, 99, 235, 0.06);
}

/* Drag and drop */
.drag-source {
  opacity: 0.3;
}

.drag-card {
  position: fixed;
  width: var(--card-width);
  height: var(--card-height);
  border: 2px solid #2563eb;
  border-radius: var(--card-radius);
  background: var(--color-background);
  color: #2563eb;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
  z-index: 200;
  pointer-events: none;
  transform: translate(-50%, -50%) rotate(3deg);
}

.drag-card .card-value {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  font-weight: 700;
}

.drag-card .card-pip {
  position: absolute;
  font-size: 0.7rem;
  font-weight: 700;
  line-height: 1;
}

.drag-card .card-pip.top-left {
  top: 4px;
  left: 6px;
}

.drag-card .card-pip.bottom-right {
  bottom: 4px;
  right: 6px;
  transform: rotate(180deg);
}

.column.drag-over {
  outline: 2px dashed #2563eb;
  outline-offset: -2px;
  border-radius: var(--card-radius);
  background: rgba(37, 99, 235, 0.06);
}

.piles.drag-over {
  outline: 2px dashed #16a34a;
  outline-offset: 4px;
  border-radius: var(--card-radius);
}

/* Destinations */
.piles {
  display: flex;
  gap: 0.5rem;
}

.dest-card {
  width: var(--card-width);
  height: var(--card-height);
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: var(--card-radius);
  font-size: 1.5rem;
  font-weight: 700;
  background: var(--color-background-soft);
  color: var(--color-text);
}

.dest-card.empty {
  border-style: dashed;
  opacity: 0.4;
  background: var(--color-background-mute);
}

/* Finished overlay */
.finished-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.4);
  z-index: 100;
}

.finished-panel {
  text-align: center;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 16px;
  padding: 2.5rem 3.5rem;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
}

.finished-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  background: #16a34a;
  color: white;
  border-radius: 50%;
}

.finished-text {
  font-size: 1.75rem;
  font-weight: 700;
}

/* Mobile */
@media (max-width: 640px) {
  .game {
    --card-width: 48px;
    --card-height: 66px;
    --card-radius: 6px;
    padding: 0.25rem;
  }

  .header {
    padding: 0.15rem 0;
  }

  .join-code {
    font-size: 0.8rem;
    padding: 0.2rem 0.5rem;
  }

  .player-list {
    font-size: 0.8rem;
  }

  .btn {
    padding: 0.35rem 0.8rem;
    font-size: 0.85rem;
  }

  .top-row {
    flex-direction: column;
    align-items: center;
    gap: 0.4rem;
    padding: 0.3rem 0;
  }

  .draw-label {
    font-size: 0.85rem;
  }

  .timer-bar-container {
    width: 100px;
  }

  .piles {
    gap: 0.25rem;
  }

  .dest-card {
    font-size: 1.1rem;
  }

  .tableau {
    gap: 0.25rem;
  }

  .card-value {
    font-size: 1.1rem;
  }

  .card-pip {
    font-size: 0.55rem;
  }

  .card-pip.top-left {
    top: 2px;
    left: 4px;
  }

  .card-pip.bottom-right {
    bottom: 2px;
    right: 4px;
  }

  .drag-card .card-value {
    font-size: 1.1rem;
  }

  .drag-card .card-pip {
    font-size: 0.55rem;
  }

  .drag-card .card-pip.top-left {
    top: 2px;
    left: 4px;
  }

  .drag-card .card-pip.bottom-right {
    bottom: 2px;
    right: 4px;
  }

  .finished-panel {
    padding: 1.5rem 2rem;
  }

  .finished-icon {
    width: 48px;
    height: 48px;
    font-size: 1.5rem;
  }

  .finished-text {
    font-size: 1.3rem;
  }
}
</style>
