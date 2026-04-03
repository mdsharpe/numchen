<template>
  <div class="lobby">
    <h1 class="title">Numchen</h1>

    <div v-if="!isConnected && !error" class="connecting">
      <div class="spinner"></div>
      <div>Connecting to server...</div>
      <div v-if="isSlow" class="connecting-hint">The server is waking up, this may take a moment</div>
    </div>

    <div v-else class="panel">
      <div class="field">
        <label for="playerName">Your name</label>
        <input
          id="playerName"
          v-model="playerName"
          maxlength="20"
          placeholder="Enter your name"
        />
      </div>

      <div class="actions">
        <button class="btn primary" :disabled="!playerName" @click="createGame">
          Create Game
        </button>

        <div class="divider"><span>or join an existing game</span></div>

        <div class="join-row">
          <input
            v-model="joinCode"
            placeholder="Code"
            maxlength="4"
            class="code-input"
          />
          <button class="btn" :disabled="!playerName || !joinCode" @click="joinGame">
            Join
          </button>
        </div>
      </div>

      <div v-if="error" class="error">{{ error }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getGameHub } from "@/gameHub";

const router = useRouter();
const hub = getGameHub();

const route = useRoute();
const playerName = ref("");
const joinCode = ref((route.query.joinCode as string) ?? "");
const isConnected = ref(false);
const isSlow = ref(false);
const error = ref("");

onMounted(async () => {
  const slowTimer = setTimeout(() => {
    isSlow.value = true;
  }, 3000);

  try {
    await hub.start();
    isConnected.value = true;
  } catch (e) {
    error.value = "Failed to connect to server. Please try refreshing the page.";
  } finally {
    clearTimeout(slowTimer);
  }
});

async function createGame() {
  try {
    const result = await hub.createGame(playerName.value);
    sessionStorage.setItem(
      "numchen_session",
      JSON.stringify({ joinCode: result.joinCode, playerId: result.playerId }),
    );
    router.push({
      name: "game",
      params: { joinCode: result.joinCode },
    });
  } catch (e: any) {
    error.value = e.message;
  }
}

async function joinGame() {
  try {
    const result = await hub.joinGame(joinCode.value, playerName.value);
    sessionStorage.setItem(
      "numchen_session",
      JSON.stringify({ joinCode: joinCode.value, playerId: result.playerId }),
    );
    router.push({
      name: "game",
      params: { joinCode: joinCode.value },
    });
  } catch (e: any) {
    error.value = e.message;
  }
}
</script>

<style scoped>
.lobby {
  max-width: 380px;
  margin: 4rem auto;
  text-align: center;
}

.title {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  margin-bottom: 2rem;
}

.connecting {
  color: var(--color-text);
  opacity: 0.6;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--color-border);
  border-top-color: #2563eb;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.connecting-hint {
  font-size: 0.85rem;
  opacity: 0.7;
}

.panel {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 2rem;
}

.field {
  margin-bottom: 1.5rem;
}

.field label {
  display: block;
  font-size: 0.9rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 0.4rem;
  opacity: 0.7;
}

.field input,
.code-input {
  width: 100%;
  padding: 0.6rem 0.8rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 1rem;
  outline: none;
  transition: border-color 0.2s;
}

.field input:focus,
.code-input:focus {
  border-color: var(--color-border-hover);
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.btn {
  padding: 0.6rem 1.2rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-background);
  color: var(--color-text);
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s;
}

.btn:hover:not(:disabled) {
  border-color: var(--color-border-hover);
  background: var(--color-background-mute);
}

.btn:disabled {
  opacity: 0.4;
  cursor: default;
}

.btn.primary {
  background: #2563eb;
  border-color: #2563eb;
  color: white;
}

.btn.primary:hover:not(:disabled) {
  background: #1d4ed8;
  border-color: #1d4ed8;
}

.divider {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.9rem;
  opacity: 0.5;
}

.divider::before,
.divider::after {
  content: "";
  flex: 1;
  height: 1px;
  background: var(--color-border);
}

.join-row {
  display: flex;
  gap: 0.5rem;
}

.code-input {
  width: 7rem;
  text-transform: uppercase;
  text-align: center;
  font-weight: 600;
  letter-spacing: 0.15em;
}

.error {
  color: #dc2626;
  margin-top: 1rem;
  font-size: 0.875rem;
}

@media (max-width: 640px) {
  .lobby {
    margin: 2rem auto;
    padding: 0 1rem;
  }

  .title {
    font-size: 2rem;
    margin-bottom: 1.5rem;
  }

  .panel {
    padding: 1.5rem;
  }
}
</style>
