import React from 'react';
import { Settings, Moon, Sun, Bell, Shield, Database } from 'lucide-react';

interface SettingsViewProps {
  isDarkMode: boolean;
  setIsDarkMode: (val: boolean) => void;
}

export const SettingsView: React.FC<SettingsViewProps> = ({ isDarkMode, setIsDarkMode }) => {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold">Paramètres de l'application</h2>
        <p className="text-xs opacity-75 mt-0.5">Personnalisez les préférences d'affichage et les options d'inventaire d'ATLAS PC.</p>
      </div>

      <div className={`p-6 rounded-xl border space-y-6 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
        <div className="flex items-center justify-between pb-4 border-b border-inherit">
          <div className="space-y-0.5">
            <h3 className="text-sm font-bold flex items-center gap-2">
              {isDarkMode ? <Moon className="w-4 h-4 text-cyan-400" /> : <Sun className="w-4 h-4 text-amber-500" />}
              <span>Thème de l'interface</span>
            </h3>
            <p className="text-xs opacity-75">Basculez instantanément entre le thème clair et le thème sombre professionnel.</p>
          </div>
          <button
            onClick={() => setIsDarkMode(!isDarkMode)}
            className="px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold shadow-md transition-all active:scale-95"
          >
            Passer au mode {isDarkMode ? 'Clair' : 'Sombre'}
          </button>
        </div>

        <div className="flex items-center justify-between pb-4 border-b border-inherit">
          <div className="space-y-0.5">
            <h3 className="text-sm font-bold flex items-center gap-2">
              <Bell className="w-4 h-4 text-blue-500" />
              <span>Inventaires automatiques</span>
            </h3>
            <p className="text-xs opacity-75">Effectuer un inventaire discret en arrière-plan à chaque démarrage.</p>
          </div>
          <input type="checkbox" defaultChecked className="w-4 h-4 accent-blue-600 cursor-pointer" />
        </div>

        <div className="flex items-center justify-between">
          <div className="space-y-0.5">
            <h3 className="text-sm font-bold flex items-center gap-2">
              <Database className="w-4 h-4 text-blue-500" />
              <span>Conservation des données</span>
            </h3>
            <p className="text-xs opacity-75">Conserver l'historique des inventaires sur 12 mois maximum.</p>
          </div>
          <select className={`p-2 rounded-xl text-xs border ${isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-100' : 'bg-slate-50 border-slate-300'}`}>
            <option>12 mois</option>
            <option>6 mois</option>
            <option>Illimité</option>
          </select>
        </div>
      </div>
    </div>
  );
};
