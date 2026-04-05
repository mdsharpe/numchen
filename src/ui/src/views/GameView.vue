<template>
  <div class="game">
    <div class="header">
      <div class="header-left">
        <a class="home-link" href="/" @click.prevent="goHome">Numchen</a>
        <span class="join-code">{{ joinCode }}</span>
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
          v-if="!gameFinished && isDev"
          class="btn"
          :class="{ active: autoPlay }"
          @click="toggleAutoPlay"
        >
          {{ autoPlay ? "Auto Play: ON" : "Auto Play" }}
        </button>
      </div>
    </div>

    <div class="board-layout">
    <div class="board-main">

    <div class="top-row">
      <div class="draw-area">
        <div v-if="currentCard !== null && !hasPlaced" class="draw-pile">
          <Transition name="card-draw" appear>
            <div
              :key="currentCard"
              class="drawn-card"
              :class="{ 'drag-source': drag?.type === 'drawn' && drag.isDragging, 'tilted': hoverColumn !== null && currentCard !== null && !hasPlaced, 'shimmer': currentCard !== null && !hasPlaced && canMoveToDestination(currentCard) }"
              :style="getCardStyle(currentCard!)"
              @pointerdown="onDrawnPointerDown"
            >
              <span class="card-pip top-left">{{ currentCard }}</span>
              <span class="card-value">{{ currentCard }}</span>
              <span class="card-pip bottom-right">{{ currentCard }}</span>
            </div>
          </Transition>
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
          <div class="draw-label">Waiting…</div>
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

      <div class="piles" :class="{ 'drag-over': dragOverDestinations, 'drop-target': currentCard !== null && !hasPlaced && !isProcessing && canMoveToDestination(currentCard) }" data-drop="destinations" @click="onPlaceDrawnCardToDestination">
        <div v-for="(pile, index) in destinations" :key="index" class="pile" data-drop="destinations">
          <Transition name="dest-pop">
            <div :key="pile" class="dest-card" :class="{ empty: pile === 0 }" :style="pile > 0 ? getCardStyle(pile) : undefined" data-drop="destinations">
              {{ pile > 0 ? pile : "" }}
            </div>
          </Transition>
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
        <TransitionGroup tag="div" class="card-stack" name="card-place">
          <div v-if="column.length === 0" key="empty" class="card empty-placeholder"></div>
          <div
            v-for="(card, cardIndex) in column"
            :key="cardIndex"
            class="card"
            :class="{
              'top-card': cardIndex === column.length - 1 && !isProcessing && !gameFinished && canMoveToDestination(card),
              'drag-source': drag?.type === 'column' && drag.columnIndex === index && drag.isDragging && cardIndex === column.length - 1,
            }"
            :style="{ top: cardIndex * getCardOffset(column.length) + 'px', zIndex: cardIndex, ...getCardStyle(card) }"
            :data-column-index="index"
            @click="onCardClick($event, index, cardIndex)"
            @pointerdown="cardIndex === column.length - 1 && !gameFinished && canMoveToDestination(card) && onColumnCardPointerDown($event, index, card)"
          >
            <span class="card-pip top-left">{{ card }}</span>
            <span class="card-value">{{ card }}</span>
            <span class="card-pip bottom-right">{{ card }}</span>
          </div>
        </TransitionGroup>
      </div>
    </div>

    </div><!-- .board-main -->

    <div class="player-sidebar">
      <div class="sidebar-heading">Players</div>
      <TransitionGroup name="player-sort" tag="div" class="sidebar-list">
        <div v-for="p in sortedPlayers" :key="p.id" class="sidebar-player">
          <span class="placed-dot" :class="{ active: p.hasPlaced }"></span>
          <span class="sidebar-player-name">{{ p.name }}</span>
          <span class="sidebar-player-score">{{ p.score }}</span>
        </div>
      </TransitionGroup>
      <div v-if="hasPlaced && waitingFor.length > 0" class="sidebar-waiting">
        Waiting for {{ waitingFor.join(', ') }}…
      </div>
    </div>

    </div><!-- .board-layout -->

    <div
      v-if="drag && drag.isDragging"
      class="drag-card"
      :style="{ left: drag.x + 'px', top: drag.y + 'px', ...getCardStyle(drag.cardValue) }"
    >
      <span class="card-pip top-left">{{ drag.cardValue }}</span>
      <span class="card-value">{{ drag.cardValue }}</span>
      <span class="card-pip bottom-right">{{ drag.cardValue }}</span>
    </div>

    <Transition name="conn-banner">
      <div v-if="connectionStatus !== 'connected'" class="connection-banner" :class="connectionStatus">
        <template v-if="connectionStatus === 'reconnecting'">Connection lost — reconnecting...</template>
        <template v-else-if="connectionStatus === 'disconnected'">
          Disconnected from server.
          <button class="conn-retry-btn" @click="retryConnection">Rejoin</button>
        </template>
      </div>
    </Transition>

    <div v-if="gameFinished" class="finished-overlay">
      <div class="finished-panel">
        <div class="finished-title">Game Over!</div>
        <div class="finished-standings">
          <div
            v-for="(p, index) in sortedPlayers"
            :key="p.id"
            class="standing-row"
            :class="{ 'is-me': p.id === myPlayerId, 'is-winner': p.score === sortedPlayers[0]?.score }"
          >
            <span class="standing-rank">{{ p.score === sortedPlayers[0]?.score ? '&#x1F451;' : `#${index + 1}` }}</span>
            <span class="standing-name">{{ p.name }}</span>
            <span class="standing-score">{{ p.score }} / {{ totalCards }}</span>
          </div>
        </div>
        <div class="finished-actions">
          <button class="btn primary" @click="restartGame">Play Again</button>
          <button class="btn" @click="goHome">Leave</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router";
import { getGameHub } from "@/gameHub";

const route = useRoute();
const router = useRouter();
const hub = getGameHub();

const isDev = import.meta.env.DEV;
const PLACEMENT_TIMEOUT_SECONDS = 45;

interface PlayerInfo {
  id: string;
  name: string;
  score: number;
  hasPlaced: boolean;
}

const joinCode = route.params.joinCode as string;
const playerInfos = ref<PlayerInfo[]>([]);
const myPlayerId = ref("");
const myName = ref("");
const totalCards = ref(96);
const connectionStatus = ref<"connected" | "reconnecting" | "disconnected">("connected");
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
const windowHeight = ref(window.innerHeight);

function onWindowResize() {
  windowWidth.value = window.innerWidth;
  windowHeight.value = window.innerHeight;
}

const sortedPlayers = computed(() =>
  [...playerInfos.value].sort((a, b) => b.score - a.score)
);

const waitingFor = computed(() =>
  playerInfos.value.filter(p => !p.hasPlaced).map(p => p.name)
);

function getCardStyle(cardNumber: number) {
  return {
    color: `var(--card-color-${cardNumber})`,
    backgroundColor: `var(--card-bg-${cardNumber})`,
    borderColor: `var(--card-border-${cardNumber})`,
  };
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

function onPlayerJoined(playerId: string, playerName: string) {
  playerInfos.value.push({ id: playerId, name: playerName, score: 0, hasPlaced: false });
}

function onPlayerLeft(playerId: string, _playerName: string) {
  playerInfos.value = playerInfos.value.filter(p => p.id !== playerId);
}

function onPlayerPlaced(playerId: string, playerName: string) {
  const p = playerInfos.value.find(p => p.id === playerId);
  if (p) {
    p.hasPlaced = true;
  }
}

function onPlayerScored(playerId: string, _playerName: string, score: number) {
  const p = playerInfos.value.find(p => p.id === playerId);
  if (p) {
    p.score = score;
  }
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
  const me = playerInfos.value.find(p => p.id === myPlayerId.value);
  if (me) {
    me.hasPlaced = true;
  }
}

function onCardDrawn(cardValue: number, deadline: number | null, scores: Record<string, number>) {
  gameStarted.value = true;
  currentCard.value = cardValue;
  hasPlaced.value = false;
  autoPlayGeneration++;
  startCountdown(deadline);
  for (const p of playerInfos.value) {
    p.hasPlaced = false;
    if (scores && scores[p.id] !== undefined) {
      p.score = scores[p.id]!;
    }
  }

  if (autoPlay.value) {
    scheduleAutoPlay(autoPlayGeneration);
  }
}

function onGameFinished() {
  gameFinished.value = true;
  currentCard.value = null;
  stopCountdown();
}

function onGameRestarted(cardValue: number, deadline: number | null, scores: Record<string, number>) {
  gameFinished.value = false;
  currentCard.value = cardValue;
  hasPlaced.value = false;
  columns.value = [[], [], [], [], [], []];
  destinations.value = [0, 0, 0, 0, 0, 0];

  for (const p of playerInfos.value) {
    p.score = scores[p.id] ?? 0;
    p.hasPlaced = false;
  }

  startCountdown(deadline);

  if (autoPlay.value) {
    scheduleAutoPlay(autoPlayGeneration);
  }
}

async function restartGame() {
  try {
    await hub.start();
    await hub.restartGame();
  } catch (e) {
    console.error("Failed to restart game:", e);
  }
}

async function retryConnection() {
  connectionStatus.value = "reconnecting";
  try {
    await hub.start();
    const saved = getSavedSession();
    if (saved) {
      await hub.rejoinGame(saved.playerId);
    }
    connectionStatus.value = "connected";
  } catch {
    connectionStatus.value = "disconnected";
  }
}

function getSavedSession(): { joinCode: string; playerId: string; playerName: string; totalCards?: number } | null {
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
    myPlayerId.value = saved.playerId;
    myName.value = saved.playerName ?? "";
    totalCards.value = result.totalCards;
    playerInfos.value = result.players.map(p => ({
      id: p.id,
      name: p.name,
      score: p.score,
      hasPlaced: result.placedPlayers.includes(p.id),
    }));
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
  hub.on("GameRestarted", onGameRestarted);
  hub.on("PlayerPlaced", onPlayerPlaced);
  hub.on("PlayerScored", onPlayerScored);

  hub.onReconnecting(() => {
    connectionStatus.value = "reconnecting";
  });

  hub.onReconnected(async () => {
    const saved = getSavedSession();
    if (saved) {
      try {
        await hub.rejoinGame(saved.playerId);
        connectionStatus.value = "connected";
      } catch (e) {
        console.error("Failed to rejoin after reconnect:", e);
        connectionStatus.value = "disconnected";
      }
    } else {
      connectionStatus.value = "disconnected";
    }
  });

  hub.onClose(() => {
    connectionStatus.value = "disconnected";
  });

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
  hub.off("GameRestarted", onGameRestarted);
  hub.off("PlayerPlaced", onPlayerPlaced);
  hub.off("PlayerScored", onPlayerScored);
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
  pointerType: string;
}

const hoverColumn = ref<number | null>(null);
const drag = ref<DragState | null>(null);
const dragOverColumn = ref<number | null>(null);
const dragOverDestinations = ref(false);
const DRAG_THRESHOLD = 5;
const TOUCH_DRAG_THRESHOLD = 12;

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
    pointerType: e.pointerType,
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
  const threshold = drag.value.pointerType === "touch" ? TOUCH_DRAG_THRESHOLD : DRAG_THRESHOLD;
  if (!drag.value.isDragging && Math.sqrt(dx * dx + dy * dy) >= threshold) {
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
  const overDestinations = !!el.closest("[data-drop='destinations']");
  dragOverDestinations.value = overDestinations && (
    drag.value?.type === "column" ||
    (drag.value?.type === "drawn" && currentCard.value !== null && canMoveToDestination(currentCard.value))
  );
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
    const destEl = (el as HTMLElement).closest("[data-drop='destinations']");
    if (destEl) {
      onPlaceDrawnCardToDestination();
      return;
    }
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
  const compact = windowWidth.value < 640 || windowHeight.value < 480;
  const cardHeight = compact ? 28 : 40;
  const maxStackHeight = compact ? 200 : 280;
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
      moved = await onTopCardClick(colIndex);
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

onBeforeRouteLeave((_to, _from, next) => {
  if (gameStarted.value && !gameFinished.value) {
    if (!confirm("Leave this game? Your progress will be lost.")) {
      next(false);
      return;
    }
  }
  sessionStorage.removeItem("numchen_session");
  next();
});

function goHome() {
  router.push({ name: "lobby" });
}

async function startGame() {
  await hub.startGame();
}

async function onPlaceCard(index: number) {
  if (currentCard.value === null || hasPlaced.value || isProcessing.value) {
    return;
  }

  isProcessing.value = true;
  const card = currentCard.value;
  try {
    columns.value[index]!.push(card);
    hasPlaced.value = true;
    currentCard.value = null;
    const me = playerInfos.value.find(p => p.id === myPlayerId.value);
    if (me) {
      me.hasPlaced = true;
    }
    await hub.placeCard(index);
  } catch (e) {
    console.error("Failed to place card:", e);
    columns.value[index]!.pop();
    hasPlaced.value = false;
    currentCard.value = card;
    const me = playerInfos.value.find(p => p.id === myPlayerId.value);
    if (me) {
      me.hasPlaced = false;
    }
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

  // If a drawn card is in hand, let the click bubble up to the column's onPlaceCard handler.
  if (currentCard.value !== null && !hasPlaced.value) {
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

async function onPlaceDrawnCardToDestination() {
  if (currentCard.value === null || hasPlaced.value || isProcessing.value) {
    return;
  }
  if (!canMoveToDestination(currentCard.value)) {
    return;
  }

  const colIndex = columns.value
    .map((col, i) => ({ i, len: col.length }))
    .sort((a, b) => a.len - b.len)[0]!.i;

  isProcessing.value = true;
  const card = currentCard.value;
  try {
    hasPlaced.value = true;
    currentCard.value = null;
    const me = playerInfos.value.find(p => p.id === myPlayerId.value);
    if (me) {
      me.hasPlaced = true;
    }
    await hub.placeCard(colIndex);
    const { pileIndex } = await hub.moveToDestination(colIndex);
    destinations.value[pileIndex] = card;
  } catch (e) {
    console.error("Failed to place drawn card to destination:", e);
    hasPlaced.value = false;
    currentCard.value = card;
    const me = playerInfos.value.find(p => p.id === myPlayerId.value);
    if (me) {
      me.hasPlaced = false;
    }
  } finally {
    isProcessing.value = false;
  }
}

async function onTopCardClick(index: number): Promise<boolean> {
  const column = columns.value[index]!;
  if (column.length === 0 || isProcessing.value || gameFinished.value) {
    return false;
  }

  isProcessing.value = true;
  try {
    const { pileIndex } = await hub.moveToDestination(index);
    const card = column.pop()!;
    destinations.value[pileIndex] = card;
    return true;
  } catch {
    // Move wasn't valid, ignore
    return false;
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

  /* Distinct card colors (1–16): dark text, light bg, medium border */
  /* Adjacent numbers use contrasting hues for easy visual scanning */
  --card-color-1: #1e3a5f; --card-bg-1: #dbeafe; --card-border-1: #93c5fd; /* blue */
  --card-color-2: #7f1d1d; --card-bg-2: #fee2e2; --card-border-2: #fca5a5; /* red */
  --card-color-3: #14532d; --card-bg-3: #dcfce7; --card-border-3: #86efac; /* green */
  --card-color-4: #4c1d95; --card-bg-4: #ede9fe; --card-border-4: #c4b5fd; /* purple */
  --card-color-5: #78350f; --card-bg-5: #fef3c7; --card-border-5: #fcd34d; /* amber */
  --card-color-6: #134e4a; --card-bg-6: #ccfbf1; --card-border-6: #5eead4; /* teal */
  --card-color-7: #831843; --card-bg-7: #fce7f3; --card-border-7: #f9a8d4; /* pink */
  --card-color-8: #365314; --card-bg-8: #ecfccb; --card-border-8: #bef264; /* lime */
  --card-color-9: #312e81; --card-bg-9: #e0e7ff; --card-border-9: #a5b4fc; /* indigo */
  --card-color-10: #7c2d12; --card-bg-10: #ffedd5; --card-border-10: #fdba74; /* orange */
  --card-color-11: #164e63; --card-bg-11: #cffafe; --card-border-11: #67e8f9; /* cyan */
  --card-color-12: #701a75; --card-bg-12: #fae8ff; --card-border-12: #f0abfc; /* magenta */
  --card-color-13: #0c4a6e; --card-bg-13: #e0f2fe; --card-border-13: #7dd3fc; /* sky */
  --card-color-14: #451a03; --card-bg-14: #fde8cd; --card-border-14: #d6a87c; /* brown */
  --card-color-15: #881337; --card-bg-15: #ffe4e6; --card-border-15: #fda4af; /* rose */
  --card-color-16: #713f12; --card-bg-16: #fef9c3; --card-border-16: #fde68a; /* gold */

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

.home-link {
  font-weight: 700;
  font-size: 1.1rem;
  color: var(--color-heading);
  text-decoration: none;
}

.home-link:hover {
  text-decoration: underline;
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

/* Board layout — main content + sidebar */
.board-layout {
  display: flex;
  flex: 1;
  min-height: 0;
  gap: 0.75rem;
}

.board-main {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 0;
}

.player-sidebar {
  width: 160px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.5rem 0;
}

.sidebar-heading {
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  opacity: 0.5;
  padding: 0 0.25rem;
}

.sidebar-list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.sidebar-player {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-background-soft);
}

.sidebar-player-name {
  font-size: 0.85rem;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
}

.sidebar-player-score {
  font-size: 0.85rem;
  font-weight: 700;
  opacity: 0.7;
}

.sidebar-waiting {
  font-size: 0.75rem;
  opacity: 0.6;
  padding: 0.25rem;
  line-height: 1.3;
}

.placed-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  border: 1.5px solid var(--color-border);
  background: transparent;
  transition: background 0.2s, border-color 0.2s;
  flex-shrink: 0;
}

.placed-dot.active {
  background: #16a34a;
  border-color: #16a34a;
}

.player-sort-move {
  transition: transform 0.4s ease;
}

.player-sort-enter-active,
.player-sort-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.player-sort-enter-from,
.player-sort-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.9);
}

/* Connection banner */
.connection-banner {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 150;
  text-align: center;
  padding: 0.5rem 1rem;
  font-size: 0.85rem;
  font-weight: 600;
}

.connection-banner.reconnecting {
  background: #f59e0b;
  color: #451a03;
}

.connection-banner.disconnected {
  background: #dc2626;
  color: white;
}

.conn-retry-btn {
  margin-left: 0.75rem;
  padding: 0.2rem 0.75rem;
  border: 1px solid rgba(255, 255, 255, 0.5);
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.15);
  color: white;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.conn-retry-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

.conn-banner-enter-active,
.conn-banner-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}

.conn-banner-enter-from,
.conn-banner-leave-to {
  transform: translateY(-100%);
  opacity: 0;
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
  border: 2px solid;
  border-radius: var(--card-radius);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
  touch-action: none;
  cursor: grab;
  transition: transform 0.25s;
}

.drawn-card.tilted {
  transform: translate(2px, 4px) rotate(3deg);
}

.drawn-card.placeholder {
  border-style: dashed;
  border-color: var(--color-border-hover);
  box-shadow: none;
  opacity: 0.6;
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
  border: 2px solid;
  border-radius: var(--card-radius);
  background: var(--color-background);
  color: var(--color-text);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
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
  border-color: var(--color-border-hover);
  opacity: 0.6;
  background: var(--color-background-mute);
  box-shadow: none;
}

@keyframes shimmer {
  0%   { background-position: -100% center; }
  100% { background-position: 200% center; }
}

.card.top-card {
  cursor: pointer;
  touch-action: none;
  outline: 2px solid rgba(255, 255, 255, 0.75);
  outline-offset: -2px;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
  background-image: linear-gradient(
    105deg,
    transparent 40%,
    rgba(255, 255, 255, 0.55) 50%,
    transparent 60%
  );
  background-size: 200% 100%;
  animation: shimmer 2.5s ease-in-out infinite;
}

/* Use ::after for the drawn-card shimmer so the parent's border is painted
   independently of the animated background, fixing the bottom-border-disappears bug. */
.drawn-card.shimmer::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: calc(var(--card-radius) - 2px);
  background-image: linear-gradient(
    105deg,
    transparent 40%,
    rgba(255, 255, 255, 0.55) 50%,
    transparent 60%
  );
  background-size: 200% 100%;
  animation: shimmer 2.5s ease-in-out infinite;
  pointer-events: none;
}

.card.top-card:hover {
  transform: translateX(-50%) translateY(-3px) scale(1.04);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

.column.drop-target,
.piles.drop-target {
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
  border: 2px solid;
  border-radius: var(--card-radius);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
  z-index: 200;
  pointer-events: none;
  transform: translate(-50%, -50%) rotate(3deg);
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
  border: 2px solid;
  border-radius: var(--card-radius);
  font-size: 1.5rem;
  font-weight: 700;
  background: var(--color-background-soft);
  color: var(--color-text);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.dest-card.empty {
  border-style: dashed;
  border-color: var(--color-border-hover);
  opacity: 0.6;
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
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: 16px;
  padding: 2rem 2.5rem;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
  min-width: 280px;
}

.finished-title {
  font-size: 1.75rem;
  font-weight: 700;
}

.finished-standings {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  width: 100%;
}

.standing-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background-soft);
}

.standing-row.is-winner {
  border-color: #ca8a04;
  background: rgba(202, 138, 4, 0.08);
}

.standing-row.is-me {
  outline: 2px solid #2563eb;
  outline-offset: -1px;
}

.standing-rank {
  font-size: 1rem;
  font-weight: 700;
  min-width: 2rem;
  text-align: center;
}

.standing-name {
  font-size: 0.95rem;
  font-weight: 600;
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.standing-score {
  font-size: 0.9rem;
  font-weight: 700;
  opacity: 0.7;
  white-space: nowrap;
}

.finished-actions {
  display: flex;
  gap: 0.75rem;
}

/* Card draw animation — new card appears in draw area */
.card-draw-enter-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}
.card-draw-enter-from {
  transform: scale(0.82) translateY(8px);
  opacity: 0;
}

/* Card place animation — card enters/leaves a column */
.card-place-enter-active {
  transition: transform 0.18s ease, opacity 0.18s ease;
}
.card-place-enter-from {
  transform: translateX(-50%) scale(0.72);
  opacity: 0;
}
.card-place-leave-active {
  transition: transform 0.15s ease, opacity 0.15s ease;
}
.card-place-leave-to {
  transform: translateX(-50%) scale(0.82);
  opacity: 0;
}

/* Destination pop — card lands on a pile */
@keyframes dest-pop {
  0%   { transform: scale(0.78); opacity: 0; }
  60%  { transform: scale(1.12); opacity: 1; }
  100% { transform: scale(1);    opacity: 1; }
}
.dest-pop-enter-active {
  animation: dest-pop 0.22s ease;
}

/* Dark-mode card colors: light text on deep-toned backgrounds */
@media (prefers-color-scheme: dark) {
  .game {
    --card-color-1: #bfdbfe; --card-bg-1: #1e3a5f; --card-border-1: #2563eb; /* blue */
    --card-color-2: #fecaca; --card-bg-2: #5c1a1a; --card-border-2: #dc2626; /* red */
    --card-color-3: #bbf7d0; --card-bg-3: #14392d; --card-border-3: #16a34a; /* green */
    --card-color-4: #ddd6fe; --card-bg-4: #3b1a6e; --card-border-4: #7c3aed; /* purple */
    --card-color-5: #fde68a; --card-bg-5: #5c3a0e; --card-border-5: #d97706; /* amber */
    --card-color-6: #99f6e4; --card-bg-6: #134040; --card-border-6: #0d9488; /* teal */
    --card-color-7: #fbcfe8; --card-bg-7: #5c1638; --card-border-7: #db2777; /* pink */
    --card-color-8: #d9f99d; --card-bg-8: #2d3d14; --card-border-8: #65a30d; /* lime */
    --card-color-9: #c7d2fe; --card-bg-9: #272560; --card-border-9: #4f46e5; /* indigo */
    --card-color-10: #fed7aa; --card-bg-10: #5c2510; --card-border-10: #ea580c; /* orange */
    --card-color-11: #a5f3fc; --card-bg-11: #134050; --card-border-11: #0891b2; /* cyan */
    --card-color-12: #f5d0fe; --card-bg-12: #4a1552; --card-border-12: #a21caf; /* magenta */
    --card-color-13: #bae6fd; --card-bg-13: #0c3a5e; --card-border-13: #0284c7; /* sky */
    --card-color-14: #e8d5c0; --card-bg-14: #3d2510; --card-border-14: #92400e; /* brown */
    --card-color-15: #fecdd3; --card-bg-15: #5c1229; --card-border-15: #e11d48; /* rose */
    --card-color-16: #fef08a; --card-bg-16: #4d3a10; --card-border-16: #ca8a04; /* gold */
  }
}

/* Narrow viewport — collapse sidebar to horizontal strip */
@media (max-width: 900px) {
  .board-layout {
    flex-direction: column;
  }

  .player-sidebar {
    order: -1;
    width: auto;
    flex-direction: row;
    align-items: center;
    gap: 0.4rem;
    padding: 0.25rem 0;
    flex-wrap: wrap;
    border-bottom: 1px solid var(--color-border);
  }

  .sidebar-heading {
    display: none;
  }

  .sidebar-list {
    flex-direction: row;
    flex-wrap: wrap;
    gap: 0.3rem;
  }

  .sidebar-player {
    padding: 0.2rem 0.4rem;
  }

  .sidebar-player-name {
    font-size: 0.75rem;
  }

  .sidebar-player-score {
    font-size: 0.75rem;
  }

  .sidebar-waiting {
    font-size: 0.7rem;
  }
}

/* Mobile — smaller cards and compact layout */
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

  .btn {
    padding: 0.35rem 0.8rem;
    font-size: 0.85rem;
  }

  .top-row {
    display: contents;
  }

  .draw-area {
    align-self: center;
    padding: 0.3rem 0;
  }

  .piles {
    order: 2;
    align-self: center;
    gap: 0.25rem;
    padding: 0.3rem 0;
    flex-shrink: 0;
  }

  .dest-card {
    font-size: 1.1rem;
  }

  .tableau {
    order: 1;
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

  .finished-panel {
    padding: 1.25rem 1.5rem;
    min-width: 240px;
  }

  .finished-title {
    font-size: 1.3rem;
  }
}

/* Landscape mobile — small height viewport (e.g. phones rotated) */
@media (max-height: 480px) {
  .game {
    --card-width: 44px;
    --card-height: 60px;
    --card-radius: 5px;
    padding: 0.15rem 0.35rem;
  }

  .header {
    padding: 0.1rem 0;
  }

  .join-code {
    font-size: 0.75rem;
    padding: 0.15rem 0.4rem;
  }

  .btn {
    padding: 0.25rem 0.6rem;
    font-size: 0.8rem;
  }

  .top-row {
    display: contents;
  }

  .draw-area {
    align-self: center;
    padding: 0.2rem 0;
  }

  .piles {
    order: 2;
    align-self: center;
    gap: 0.25rem;
    padding: 0.2rem 0;
    flex-shrink: 0;
  }

  .dest-card {
    font-size: 1rem;
  }

  .tableau {
    order: 1;
    gap: 0.25rem;
  }

  .card-value {
    font-size: 1rem;
  }

  .card-pip {
    font-size: 0.5rem;
  }

  .card-pip.top-left {
    top: 2px;
    left: 3px;
  }

  .card-pip.bottom-right {
    bottom: 2px;
    right: 3px;
  }

  .draw-label {
    font-size: 0.75rem;
  }

  .timer-bar-container {
    width: 100px;
  }

  .finished-panel {
    padding: 1rem 1.25rem;
    min-width: 220px;
  }

  .finished-title {
    font-size: 1.2rem;
  }
}
</style>
