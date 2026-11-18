import React from 'react';
import { SystemInfo } from '../../types';
import { Cpu, HardDrive, Monitor, Zap, Shield, CheckCircle2 } from 'lucide-react';

interface HardwareViewProps {
  systemInfo: SystemInfo;
  isDarkMode: boolean;
}

export const HardwareView: React.FC<HardwareViewProps> = ({ systemInfo, isDarkMode }) => {
  const hardwareItems = [
    { title: 'Processeur (CPU)', desc: systemInfo.processor, details: '8 Performance Cores, 16 Efficient Cores, Turbo Boost up to 5.4 GHz', icon: Cpu },
    { title: 'Carte Graphique (GPU)', desc: systemInfo.gpu, details: '16GB GDDR6X VRAM, Ray Tracing Cores, DLSS 3.5 Support', icon: Monitor },
    { title: 'Mémoire Vive (RAM)', desc: systemInfo.ramInstalled, details: 'DDR5 5600MHz Dual Channel (SK Hynix)', icon: Zap },
    { title: 'Stockage Principal', desc: systemInfo.storageSummary, details: 'PCIe 4.0 NVMe M.2 SSD - Vitesse lecture 7450 Mo/s', icon: HardDrive },
    { title: 'Carte Mère', desc: 'ASUS ROG STRIX Z790-E GAMING WIFI', details: 'Chipset Intel Z790, Socket LGA1700, Wi-Fi 6E', icon: Shield },
    { title: 'BIOS & UEFI', desc: systemInfo.bios, details: 'Mode UEFI sécurisé actif, TPM 2.0 activé', icon: Shield },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold">Matériel détecté</h2>
        <p className="text-xs opacity-75 mt-0.5">Spécifications détaillées des composants matériels de votre ordinateur.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {hardwareItems.map((item, i) => {
          const Icon = item.icon;
          return (
            <div key={i} className={`p-5 rounded-xl border flex items-start gap-4 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
              <div className="w-12 h-12 rounded-xl bg-blue-600/10 text-blue-500 flex items-center justify-center shrink-0 border border-blue-600/20">
                <Icon className="w-6 h-6" />
              </div>
              <div className="space-y-1 flex-1">
                <span className="text-[10px] font-mono uppercase tracking-wider text-blue-500 font-bold">{item.title}</span>
                <h4 className="text-sm font-bold">{item.desc}</h4>
                <p className="text-xs opacity-75">{item.details}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
