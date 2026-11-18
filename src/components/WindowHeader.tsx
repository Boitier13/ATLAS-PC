import React from 'react';
import { TopTab } from '../types';
import { Home, ClipboardList, Scale, BarChart3, Settings, Info, Sun, Moon, Minus, Square, X } from 'lucide-react';

interface WindowHeaderProps {
  activeTab: TopTab;
  setActiveTab: (tab: TopTab) => void;
  isDarkMode: boolean;
  setIsDarkMode: (val: boolean) => void;
  onNewInventory: () => void;
}

export const WindowHeader: React.FC<WindowHeaderProps> = ({
  activeTab,
  setActiveTab,
  isDarkMode,
  setIsDarkMode,
  onNewInventory,
}) => {
  return (
    <header className={`select-none border-b ${isDarkMode ? 'bg-slate-900 border-slate-800 text-slate-100' : 'bg-slate-100 border-slate-300 text-slate-800'}`}>
      {/* Windows Title Bar */}
      <div className={`flex items-center justify-between px-3 py-1.5 text-xs border-b ${isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-400' : 'bg-white border-slate-200 text-slate-600'}`}>
        <div className="flex items-center gap-2">
          <span className="w-4 h-4 rounded bg-cyan-600 flex items-center justify-center text-white font-bold text-[10px]">A</span>
          <span className="font-medium">ATLAS PC — Inventaire et Gestion de votre ordinateur v1.0.0</span>
        </div>

        {/* Theme Toggle & Window Controls */}
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <span className="text-[11px] font-medium">Thème :</span>
            <button
              onClick={() => setIsDarkMode(!isDarkMode)}
              className={`flex items-center gap-1.5 px-2 py-0.5 rounded text-[11px] font-medium border transition-colors ${
                isDarkMode ? 'bg-slate-800 border-slate-700 text-cyan-400' : 'bg-slate-200 border-slate-300 text-slate-700'
              }`}
            >
              {isDarkMode ? <Moon className="w-3 h-3" /> : <Sun className="w-3 h-3 text-amber-500" />}
              <span>{isDarkMode ? 'Sombre' : 'Clair'}</span>
            </button>
          </div>

          <div className="flex items-center gap-1 text-slate-500 pl-2 border-l border-slate-400/30">
            <button aria-label="Réduire la fenêtre" className="p-1 hover:bg-slate-500/20 rounded"><Minus className="w-3.5 h-3.5" /></button>
            <button aria-label="Agrandir la fenêtre" className="p-1 hover:bg-slate-500/20 rounded"><Square className="w-3 h-3" /></button>
            <button aria-label="Fermer la fenêtre" className="p-1 hover:bg-rose-500/20 hover:text-rose-500 rounded"><X className="w-3.5 h-3.5" /></button>
          </div>
        </div>
      </div>

      {/* Top Ribbon Toolbar */}
      <div className={`px-4 py-2 flex flex-wrap items-center justify-between gap-4 ${isDarkMode ? 'bg-slate-900' : 'bg-slate-100'}`}>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg bg-gradient-to-tr from-cyan-600 to-blue-600 flex items-center justify-center text-white shadow-md">
            <span className="font-extrabold text-lg">A</span>
          </div>
          <div>
            <h1 className="text-base font-bold leading-tight flex items-center gap-2">
              ATLAS PC
              <span className="text-[11px] font-normal px-1.5 py-0.2 rounded bg-cyan-500/20 text-cyan-500 border border-cyan-500/30">v1.0.0</span>
            </h1>
            <p className="text-[11px] opacity-75">Connaître · Maîtriser · Optimiser</p>
          </div>
        </div>

        {/* Ribbon Navigation Tabs */}
        <div className="flex items-center gap-1 overflow-x-auto py-1">
          {[
            { id: 'accueil', label: 'Accueil', icon: Home },
            { id: 'inventaire', label: 'Inventaires', icon: ClipboardList },
            { id: 'comparer', label: 'Comparer', icon: Scale },
            { id: 'rapports', label: 'Rapports', icon: BarChart3 },
            { id: 'parametres', label: 'Paramètres', icon: Settings },
            { id: 'apropos', label: 'À propos', icon: Info },
          ].map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as TopTab)}
                className={`flex flex-col items-center justify-center px-4 py-2 rounded-lg text-xs font-medium transition-all ${
                  isActive
                    ? isDarkMode
                      ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/40 shadow-sm'
                      : 'bg-white text-blue-600 border border-blue-300 shadow-sm font-semibold'
                    : isDarkMode
                    ? 'text-slate-300 hover:bg-slate-800 hover:text-white'
                    : 'text-slate-600 hover:bg-slate-200/80 hover:text-slate-900'
                }`}
              >
                <Icon className="w-4 h-4 mb-1" />
                <span>{tab.label}</span>
              </button>
            );
          })}
        </div>

        {/* Quick Action Button */}
        <div>
          <button
            onClick={onNewInventory}
            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-semibold shadow-md transition-all active:scale-95"
          >
            <span>Lancer un nouvel inventaire</span>
          </button>
        </div>
      </div>
    </header>
  );
};
