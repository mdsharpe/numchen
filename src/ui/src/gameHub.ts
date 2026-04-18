import {
  HttpTransportType,
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from "@microsoft/signalr";

export interface PlayerSummary {
  id: string;
  name: string;
}

export interface CreateGameResult {
  joinCode: string;
  playerId: string;
  totalCards: number;
  players: PlayerSummary[];
}

export interface JoinGameResult {
  playerId: string;
  totalCards: number;
  players: PlayerSummary[];
}

export interface RejoinedPlayerInfo {
  id: string;
  name: string;
  score: number;
}

export interface RejoinGameResult {
  joinCode: string;
  totalCards: number;
  players: RejoinedPlayerInfo[];
  placedPlayers: string[];
  gameStarted: boolean;
  gameFinishing: boolean;
  gameFinished: boolean;
  currentCard: number | null;
  hasPlaced: boolean;
  placementDeadline: number | null;
  finishingDeadline: number | null;
  columns: number[][];
  destinations: number[];
}

export interface MoveToDestinationResult {
  pileIndex: number;
}

export interface GameHubEvents {
  PlayerJoined: (playerId: string, playerName: string) => void;
  PlayerLeft: (playerId: string, playerName: string) => void;
  CardAutoPlaced: (columnIndex: number) => void;
  CardDrawn: (cardValue: number, deadline: number | null, scores: Record<string, number>) => void;
  FinishingPhaseStarted: (deadline: number | null, scores: Record<string, number>) => void;
  GameFinished: () => void;
  GameRestarted: (cardValue: number, deadline: number | null, scores: Record<string, number>) => void;
  PlayerPlaced: (playerId: string, playerName: string) => void;
  PlayerScored: (playerId: string, playerName: string, score: number) => void;
}

export class GameHubClient {
  private connection: HubConnection;

  constructor() {
    const apiUrl = import.meta.env.VITE_API_URL;
    const hubUrl = apiUrl ? `${apiUrl}/game` : "/hub/game";

    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        transport:
          HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }

  on<K extends keyof GameHubEvents>(event: K, handler: GameHubEvents[K]): void {
    this.connection.on(event, handler as (...args: any[]) => void);
  }

  off<K extends keyof GameHubEvents>(event: K, handler: GameHubEvents[K]): void {
    this.connection.off(event, handler as (...args: any[]) => void);
  }

  async createGame(playerName: string): Promise<CreateGameResult> {
    return await this.connection.invoke<CreateGameResult>("CreateGame", playerName);
  }

  async joinGame(joinCode: string, playerName: string): Promise<JoinGameResult> {
    return await this.connection.invoke<JoinGameResult>("JoinGame", joinCode, playerName);
  }

  async rejoinGame(playerId: string): Promise<RejoinGameResult> {
    return await this.connection.invoke<RejoinGameResult>(
      "RejoinGame",
      playerId,
    );
  }

  async startGame(): Promise<void> {
    await this.connection.invoke("StartGame");
  }

  async placeCard(columnIndex: number): Promise<void> {
    await this.connection.invoke("PlaceCard", columnIndex);
  }

  async moveToDestination(columnIndex: number): Promise<MoveToDestinationResult> {
    return await this.connection.invoke<MoveToDestinationResult>("MoveToDestination", columnIndex);
  }

  async restartGame(): Promise<void> {
    await this.connection.invoke("RestartGame");
  }

  onReconnected(callback: () => void): void {
    this.connection.onreconnected(callback);
  }

  onReconnecting(callback: (error?: Error) => void): void {
    this.connection.onreconnecting(callback);
  }

  onClose(callback: (error?: Error) => void): void {
    this.connection.onclose(callback);
  }

  get state(): HubConnectionState {
    return this.connection.state;
  }
}

let instance: GameHubClient | null = null;

export function getGameHub(): GameHubClient {
  if (!instance) {
    instance = new GameHubClient();
  }
  return instance;
}
