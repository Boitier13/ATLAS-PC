export type NavigationSection =
  | 'overview'
  | 'hardware'
  | 'software'
  | 'windows-apps'
  | 'system-tools'
  | 'storage'
  | 'network'
  | 'security'
  | 'performance'
  | 'inventories'
  | 'backup'
  | 'restore'
  | 'history'
  | 'delete'
  | 'settings'
  | 'about'
  | 'help';

export type TopTab = 'accueil' | 'inventaire' | 'comparer' | 'rapports' | 'parametres' | 'apropos';

export interface SystemInfo {
  computerName: string;
  manufacturer: string;
  model: string;
  bios: string;
  architecture: string;
  screenResolution: string;
  osName: string;
  osVersion: string;
  processor: string;
  ramInstalled: string;
  gpu: string;
  storageSummary: string;
  networkStatus: string;
}

export interface InstalledSoftware {
  id: string;
  name: string;
  publisher: string;
  version: string;
  size: string;
  installDate: string;
  category: string;
}

export interface InventorySnapshot {
  id: string;
  date: string;
  description: string;
  version: string;
  filesCount: number;
  totalSize: string;
  componentsCount: number;
}
