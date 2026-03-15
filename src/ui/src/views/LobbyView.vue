<template>
  <div class="lobby">
    <h1>Numchen</h1>

    <div v-if="!isConnected">Connecting...</div>

    <div v-else>
      <div class="name-input">
        <label for="playerName">Your name</label>
        <input id="playerName" v-model="playerName" maxlength="20" />
      </div>

      <div class="actions">
        <div class="join">
          <input v-model="joinCode" placeholder="Join code" maxlength="4" />
          <button :disabled="!playerName || !joinCode" @click="joinGame">Join</button>
        </div>

        <div class="divider">or</div>

        <button :disabled="!playerName" @click="createGame">Create Game</button>
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
const error = ref("");

onMounted(async () => {
  try {
    await hub.start();
    isConnected.value = true;
  } catch (e) {
    error.value = "Failed to connect to server.";
  }
});

async function createGame() {
  try {
    const code = await hub.createGame(playerName.value);
    router.push({ name: "game", params: { joinCode: code }, query: { name: playerName.value } });
  } catch (e: any) {
    error.value = e.message;
  }
}

async function joinGame() {
  try {
    await hub.joinGame(joinCode.value, playerName.value);
    router.push({ name: "game", params: { joinCode: joinCode.value }, query: { name: playerName.value } });
  } catch (e: any) {
    error.value = e.message;
  }
}
</script>

<style scoped>
.lobby {
  max-width: 400px;
  margin: 2rem auto;
  text-align: center;
}

.name-input {
  margin: 1rem 0;
}

.name-input input {
  margin-left: 0.5rem;
  padding: 0.25rem 0.5rem;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: center;
}

.join {
  display: flex;
  gap: 0.5rem;
}

.join input {
  width: 6rem;
  padding: 0.25rem 0.5rem;
  text-transform: uppercase;
}

.divider {
  color: #999;
  font-size: 0.875rem;
}

.error {
  color: red;
  margin-top: 1rem;
}
</style>
