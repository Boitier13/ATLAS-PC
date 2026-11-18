import { SystemInfo, InstalledSoftware, InventorySnapshot } from '../types';

export const SYSTEM_INFO_DEFAULT: SystemInfo = {
  computerName: 'PC-ATLAS-WORKSTATION',
  manufacturer: 'Dell Inc.',
  model: 'OptiPlex 7800 Pro',
  bios: 'Version 2.4.1 (UEFI Secure Boot)',
  architecture: '64 bits (x64-based PC)',
  screenResolution: '1920 x 1080 (32-bit, 60Hz)',
  osName: 'Windows 11 Professionnel',
  osVersion: '23H2 (Build 22631.4317)',
  processor: 'Intel(R) Core(TM) i7-13700K 3.40 GHz',
  ramInstalled: '32,0 Go (31,7 Go utilisables)',
  gpu: 'NVIDIA GeForce RTX 4070 Ti Super (16 Go)',
  storageSummary: 'SSD 2.0 To NVMe M.2 (1,42 To libres)',
  networkStatus: 'Connexion Ethernet (1 Gbps) & Wi-Fi 6E',
};

export const INSTALLED_SOFTWARE_DEFAULT: InstalledSoftware[] = [
  { id: 'sw-1', name: 'Google Chrome', publisher: 'Google LLC', version: '131.0.6778.86', size: '628 Mo', installDate: '18/11/2026', category: 'Navigateur' },
  { id: 'sw-2', name: 'Microsoft Edge', publisher: 'Microsoft Corporation', version: '131.0.2903.51', size: '725 Mo', installDate: '10/11/2026', category: 'Navigateur' },
  { id: 'sw-3', name: 'VLC media player', publisher: 'VideoLAN', version: '3.0.21', size: '142 Mo', installDate: '05/11/2026', category: 'Multimédia' },
  { id: 'sw-4', name: '7-Zip', publisher: 'Igor Pavlov', version: '24.09', size: '5,7 Mo', installDate: '01/11/2026', category: 'Utilitaire' },
  { id: 'sw-5', name: 'Notepad++ (64-bit)', publisher: 'Notepad++ Team', version: '8.8.1', size: '12,4 Mo', installDate: '28/10/2026', category: 'Développement' },
  { id: 'sw-6', name: 'Adobe Acrobat Reader DC', publisher: 'Adobe Systems Inc.', version: '25.001.20615', size: '567 Mo', installDate: '20/10/2026', category: 'Bureautique' },
  { id: 'sw-7', name: 'Visual Studio Code', publisher: 'Microsoft Corporation', version: '1.95.3', size: '1,2 Go', installDate: '15/10/2026', category: 'Développement' },
  { id: 'sw-8', name: 'Discord', publisher: 'Discord Inc.', version: '1.0.9168', size: '340 Mo', installDate: '12/10/2026', category: 'Communication' },
  { id: 'sw-9', name: 'Steam', publisher: 'Valve Corporation', version: '2.10.91', size: '4,5 Go', installDate: '08/10/2026', category: 'Jeux' },
  { id: 'sw-10', name: 'Spotify', publisher: 'Spotify AB', version: '1.2.48', size: '280 Mo', installDate: '02/10/2026', category: 'Multimédia' },
];

export const INVENTORY_HISTORY_DEFAULT: InventorySnapshot[] = [
  { id: 'inv-1', date: '18/11/2026 16:45', description: 'Inventaire manuel complet', version: 'v1.0.0', filesCount: 245, totalSize: '12,6 Go', componentsCount: 38 },
  { id: 'inv-2', date: '17/11/2026 20:12', description: 'Après nettoyage système', version: 'v1.0.0', filesCount: 243, totalSize: '12,4 Go', componentsCount: 38 },
  { id: 'inv-3', date: '15/11/2026 18:30', description: 'Inventaire automatique hebdomadaire', version: 'v1.0.0', filesCount: 240, totalSize: '12,1 Go', componentsCount: 37 },
  { id: 'inv-4', date: '10/11/2026 14:20', description: 'Mise à jour pilotes graphiques', version: 'v1.0.0', filesCount: 238, totalSize: '11,9 Go', componentsCount: 37 },
];
