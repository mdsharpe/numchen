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

    <div v-if="gameFinished" class="finished">
      <div class="finished-icon">&#10003;</div>
      <div class="finished-text">Game Over!</div>
    </div>

    <div v-if="gameStarted" class="board">
      <div v-if="currentCard !== null && !hasPlaced" class="draw-area">
        <div class="drawn-card">{{ currentCard }}</div>
        <div class="draw-label">Place this card</div>
        <div v-if="countdown !== null" class="timer-bar-container">
          <div
            class="timer-bar"
            :class="{ urgent: countdown <= 5 }"
            :style="{ width: timerPercent + '%' }"
          ></div>
        </div>
      </div>
      <div v-else-if="hasPlaced" class="draw-area waiting">
        <div class="draw-label">Waiting for other players...</div>
        <div v-if="countdown !== null" class="timer-bar-container">
          <div
            class="timer-bar"
            :style="{ width: timerPercent + '%' }"
          ></div>
        </div>
      </div>

      <div class="columns">
        <div v-for="(column, index) in columns" :key="index" class="column">
          <div class="column-label">{{ index + 1 }}</div>
          <div class="card-stack">
            <div
              v-for="(card, cardIndex) in column"
              :key="cardIndex"
              class="card"
              :class="{
                'top-card': cardIndex === column.length - 1 && !isProcessing && canMoveToDestination(card),
              }"
              @click="cardIndex === column.length - 1 && !isProcessing && onTopCardClick(index)"
            >
              {{ card }}
            </div>
          </div>
          <div
            v-if="currentCard !== null && !hasPlaced"
            class="drop-zone"
            :class="{ disabled: isProcessing }"
            @click="onPlaceCard(index)"
          >
            &#9660;
          </div>
        </div>
      </div>

      <div class="destinations">
        <div class="destinations-label">Destination Piles</div>
        <div class="piles">
          <div v-for="(pile, index) in destinations" :key="index" class="pile">
            <div class="dest-card" :class="{ empty: pile === 0 }">
              {{ pile > 0 ? pile : "" }}
            </div>
          </div>
        </div>
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

function onCardAutoPlaced(columnIndex: number) {
  if (currentCard.value === null || hasPlaced.value) {
    return;
  }

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
  hub.off("PlayerJoined", onPlayerJoined);
  hub.off("PlayerLeft", onPlayerLeft);
  hub.off("CardAutoPlaced", onCardAutoPlaced);
  hub.off("CardDrawn", onCardDrawn);
  hub.off("GameFinished", onGameFinished);
  stopCountdown();
});

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

async function onTopCardClick(index: number) {
  const column = columns.value[index]!;
  if (column.length === 0 || isProcessing.value) {
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
  max-width: 800px;
  margin: 0 auto;
  padding: 1rem;
}

/* Header */
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: 1.5rem;
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
  padding: 0.2rem 0.6rem;
  font-weight: 700;
  font-size: 0.8rem;
  letter-spacing: 0.1em;
}

.player-list {
  font-size: 0.85rem;
  opacity: 0.7;
}

.btn {
  padding: 0.45rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 0.8rem;
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

/* Board */
.board {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
}

/* Draw area */
.draw-area {
  text-align: center;
  height: 130px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
}

.drawn-card {
  width: 64px;
  height: 88px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  font-weight: 700;
  border: 2px solid #2563eb;
  border-radius: 10px;
  background: var(--color-background);
  color: #2563eb;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.2);
}

.draw-label {
  font-size: 0.8rem;
  opacity: 0.6;
  font-weight: 500;
}

.timer-bar-container {
  width: 120px;
  height: 4px;
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

/* Columns */
.columns {
  display: flex;
  gap: 0.75rem;
}

.column {
  text-align: center;
  min-width: 56px;
}

.column-label {
  font-size: 0.7rem;
  font-weight: 600;
  opacity: 0.4;
  text-transform: uppercase;
  margin-bottom: 0.4rem;
}

.card-stack {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-height: 40px;
}

.card {
  width: 52px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 700;
  background: var(--color-background);
  color: var(--color-text);
  user-select: none;
}

.card.top-card {
  cursor: pointer;
  border-color: #16a34a;
  color: #16a34a;
  box-shadow: 0 0 0 1px rgba(22, 163, 74, 0.15);
  transition: all 0.15s;
}

.card.top-card:hover {
  background: rgba(22, 163, 74, 0.08);
  box-shadow: 0 0 0 2px rgba(22, 163, 74, 0.25);
}

.drop-zone {
  cursor: pointer;
  margin-top: 4px;
  width: 52px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px dashed #2563eb;
  border-radius: 6px;
  color: #2563eb;
  font-size: 0.7rem;
  background: transparent;
  transition: all 0.15s;
  opacity: 0.5;
}

.drop-zone:hover:not(.disabled) {
  opacity: 1;
  background: rgba(37, 99, 235, 0.06);
}

.drop-zone.disabled {
  cursor: default;
  opacity: 0.25;
}

/* Destinations */
.destinations {
  text-align: center;
}

.destinations-label {
  font-size: 0.7rem;
  font-weight: 600;
  opacity: 0.4;
  text-transform: uppercase;
  margin-bottom: 0.5rem;
}

.piles {
  display: flex;
  gap: 0.5rem;
}

.dest-card {
  width: 52px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 700;
  background: var(--color-background-soft);
  color: var(--color-text);
}

.dest-card.empty {
  border-style: dashed;
  opacity: 0.3;
}

/* Finished */
.finished {
  text-align: center;
  margin: 3rem 0;
}

.finished-icon {
  width: 56px;
  height: 56px;
  margin: 0 auto 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  background: #16a34a;
  color: white;
  border-radius: 50%;
}

.finished-text {
  font-size: 1.5rem;
  font-weight: 700;
}
</style>
