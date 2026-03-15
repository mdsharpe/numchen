<template>
  <div class="game">
    <div class="header">
      <span class="join-code">Code: {{ joinCode }}</span>
      <span class="players">Players: {{ players.join(", ") }}</span>
      <button v-if="!gameStarted" @click="startGame">Start Game</button>
    </div>

    <div v-if="gameFinished" class="finished">Game Over!</div>

    <div v-if="gameStarted" class="board">
      <div v-if="currentCard !== null && !hasPlaced" class="current-card">
        <div class="card drawn">{{ currentCard }}</div>
        <div class="label">Place this card</div>
      </div>
      <div v-else-if="hasPlaced" class="current-card">
        <div class="label">Waiting for other players...</div>
      </div>

      <div class="columns">
        <div
          v-for="(column, index) in columns"
          :key="index"
          class="column"
          @click="onColumnClick(index)"
        >
          <div class="column-header">Col {{ index + 1 }}</div>
          <div class="card-stack">
            <div v-for="(card, cardIndex) in column" :key="cardIndex" class="card">
              {{ card }}
            </div>
            <div v-if="column.length === 0" class="card empty">-</div>
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
const myName = route.query.name as string;
const players = ref<string[]>([myName]);
const gameStarted = ref(false);
const gameFinished = ref(false);
const currentCard = ref<number | null>(null);
const hasPlaced = ref(false);
const columns = ref<number[][]>([[], [], [], [], [], []]);
const destinations = ref<number[]>([0, 0, 0, 0, 0, 0]);

function onPlayerJoined(playerName: string) {
  players.value.push(playerName);
}

function onCardDrawn(cardValue: number) {
  gameStarted.value = true;
  currentCard.value = cardValue;
  hasPlaced.value = false;
}

function onGameFinished() {
  gameFinished.value = true;
  currentCard.value = null;
}

onMounted(() => {
  if (!myName) {
    router.replace({ name: "lobby", query: { joinCode } });
    return;
  }

  hub.on("PlayerJoined", onPlayerJoined);
  hub.on("CardDrawn", onCardDrawn);
  hub.on("GameFinished", onGameFinished);
});

onUnmounted(() => {
  hub.off("PlayerJoined", onPlayerJoined);
  hub.off("CardDrawn", onCardDrawn);
  hub.off("GameFinished", onGameFinished);
});

async function startGame() {
  await hub.startGame();
}

async function onColumnClick(index: number) {
  const column = columns.value[index]!;

  if (currentCard.value !== null && !hasPlaced.value) {
    // Place the drawn card
    await hub.placeCard(index);
    column.push(currentCard.value);
    hasPlaced.value = true;
    currentCard.value = null;
  } else {
    // Try to move top card to destination
    if (column.length === 0) {
      return;
    }

    try {
      await hub.moveToDestination(index);
      const card = column.pop()!;
      // Find the right destination pile
      if (card === 1) {
        const emptyPile = destinations.value.indexOf(0);
        if (emptyPile !== -1) {
          destinations.value[emptyPile] = card;
        }
      } else {
        const pileIndex = destinations.value.indexOf(card - 1);
        if (pileIndex !== -1) {
          destinations.value[pileIndex] = card;
        }
      }
    } catch {
      // Move wasn't valid, ignore
    }
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
  cursor: pointer;
  text-align: center;
  min-width: 60px;
}

.column:hover {
  background-color: #f0f0f0;
  border-radius: 4px;
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
}

.card.drawn {
  font-size: 1.5rem;
  padding: 1rem;
  border-color: blue;
  color: blue;
}

.card.empty {
  border-style: dashed;
  color: #999;
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

.finished {
  text-align: center;
  font-size: 2rem;
  font-weight: bold;
  color: green;
  margin: 2rem;
}
</style>
