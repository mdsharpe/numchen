<template>
  <div class="game">
    <div class="header">
      <span class="join-code">Code: {{ joinCode }}</span>
      <span class="players">Players: {{ players.join(", ") }}</span>
      <button v-if="!gameStarted" @click="startGame">Start Game</button>
      <button v-if="!gameFinished" @click="toggleAutoPlay" :class="{ active: autoPlay }">
        {{ autoPlay ? "Auto Play: ON" : "Auto Play: OFF" }}
      </button>
    </div>

    <div v-if="gameFinished" class="finished">Game Over!</div>

    <div v-if="gameStarted" class="board">
      <div v-if="currentCard !== null && !hasPlaced" class="current-card">
        <div class="card drawn">{{ currentCard }}</div>
        <div class="label">Place this card</div>
        <div v-if="countdown !== null" class="countdown" :class="{ urgent: countdown <= 5 }">
          {{ countdown }}s
        </div>
      </div>
      <div v-else-if="hasPlaced" class="current-card">
        <div class="label">Waiting for other players...</div>
        <div v-if="countdown !== null" class="countdown">{{ countdown }}s</div>
      </div>

      <div class="columns">
        <div v-for="(column, index) in columns" :key="index" class="column">
          <div class="column-header">Col {{ index + 1 }}</div>
          <div class="card-stack">
            <div
              v-for="(card, cardIndex) in column"
              :key="cardIndex"
              class="card"
              :class="{
                'top-card': cardIndex === column.length - 1,
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
            Place here
          </div>
        </div>
      </div>

      <div class="destinations">
        <div class="label">Destination Piles</div>
        <div class="piles">
          <div v-for="(pile, index) in destinations" :key="index" class="pile">
            <div class="card" :class="{ empty: pile === 0 }">
              {{ pile > 0 ? pile : "-" }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getGameHub } from "@/gameHub";

const route = useRoute();
const router = useRouter();
const hub = getGameHub();

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
  margin: 1rem auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 1rem;
  border-bottom: 1px solid #ccc;
  margin-bottom: 1rem;
}

.board {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
}

.current-card {
  text-align: center;
}

.columns {
  display: flex;
  gap: 1rem;
}

.column {
  text-align: center;
  min-width: 60px;
}

.column-header {
  font-weight: bold;
  margin-bottom: 0.5rem;
}

.card-stack {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.card {
  border: 1px solid #333;
  border-radius: 4px;
  padding: 0.5rem;
  min-width: 40px;
  text-align: center;
  font-weight: bold;
  background: white;
  color: #333;
}

.card.drawn {
  font-size: 1.5rem;
  padding: 1rem;
  border-color: blue;
  color: blue;
}

.card.top-card {
  cursor: pointer;
  border-color: #666;
}

.card.top-card:hover {
  background-color: #e8f5e9;
  border-color: green;
}

.card.empty {
  border-style: dashed;
  color: #999;
}

.drop-zone {
  cursor: pointer;
  margin-top: 4px;
  padding: 0.5rem;
  border: 2px dashed blue;
  border-radius: 4px;
  color: blue;
  font-size: 0.75rem;
  background: #e3f2fd;
}

.drop-zone:hover:not(.disabled) {
  background: #bbdefb;
}

.drop-zone.disabled {
  cursor: default;
  opacity: 0.5;
}

.destinations {
  text-align: center;
}

.piles {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.label {
  font-size: 0.875rem;
  color: #666;
  margin-top: 0.5rem;
}

.countdown {
  font-size: 0.875rem;
  color: #666;
  margin-top: 0.25rem;
}

.countdown.urgent {
  color: #d32f2f;
  font-weight: bold;
}

button.active {
  background-color: #4caf50;
  color: white;
  border-color: #388e3c;
}

.finished {
  text-align: center;
  font-size: 2rem;
  font-weight: bold;
  color: green;
  margin: 2rem;
}
</style>
