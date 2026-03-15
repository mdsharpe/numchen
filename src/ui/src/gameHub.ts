import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from "@microsoft/signalr";

export interface GameHubEvents {
  PlayerJoined: (playerName: string) => void;
  CardDrawn: (cardValue: number) => void;
  GameFinished: () => void;
}

export class GameHubClient {
  private connection: HubConnection;

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl("/hub/game")
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

  async createGame(playerName: string): Promise<string> {
    return await this.connection.invoke<string>("CreateGame", playerName);
  }

  async joinGame(joinCode: string, playerName: string): Promise<void> {
    await this.connection.invoke("JoinGame", joinCode, playerName);
  }

  async startGame(): Promise<void> {
    await this.connection.invoke("StartGame");
  }

  async placeCard(columnIndex: number): Promise<void> {
    await this.connection.invoke("PlaceCard", columnIndex);
  }

  async moveToDestination(columnIndex: number): Promise<void> {
    await this.connection.invoke("MoveToDestination", columnIndex);
  }
}

let instance: GameHubClient | null = null;

export function getGameHub(): GameHubClient {
  if (!instance) {
    instance = new GameHubClient();
  }
  return instance;
}
