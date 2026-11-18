import React from 'react';
import { NavigationSection } from '../types';
import { LayoutDashboard, Cpu, Layers, HardDrive, Wifi, Shield, Activity, BookmarkPlus, RotateCcw, Trash2, History, Settings, HelpCircle, LogOut } from 'lucide-react';

interface LeftSidebarProps {
  currentSection: NavigationSection;
  setCurrentSection: (sec: NavigationSection) => void;
  isDarkMode: boolean;
}

export const LeftSidebar: React.FC<LeftSidebarProps> = ({ currentSection, setCurrentSection, isDarkMode }) => {
  const menuItems = [
    { id: 'overview', label: "Vue d'ensemble", icon: LayoutDashboard },
    { id: 'hardware', label: 'Matériel', icon: Cpu },
    { id: 'software', label: 'Logiciels installés', icon: Layers },
    { id: 'storage', label: 'Stockage', icon: HardDrive },
    { id: 'network', label: 'Réseau', icon: Wifi },
    { id: 'security', label: 'Sécurité', icon: Shield },
    { id: 'performance', label: 'Performances', icon: Activity },
  ];

  const inventoryItems = [
    { id: 'backup', label: 'Sauvegarder', icon: BookmarkPlus },
    { id: 'restore', label: 'Restaurer', icon: RotateCcw },
    { id: 'history', label: 'Historique', icon: History },
    { id: 'delete', label: 'Supprimer', icon: Trash2 },
  ];

  const systemItems = [
    { id: 'settings', label: 'Paramètres', icon: Settings },
    { id: 'help', label: 'Aide', icon: HelpCircle },
    { id: 'about', label: 'Quitter', icon: LogOut },
  ];

  return (
    <aside className={`w-64 shrink-0 flex flex-col border-r select-none ${isDarkMode ? 'bg-slate-900 border-slate-800 text-slate-200' : 'bg-slate-100 border-slate-300 text-slate-700'}`}>
      <div className="flex-1 overflow-y-auto py-3 px-2 space-y-6">
        {/* Consultation Section */}
        <div>
          <div className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 font-mono">
            Consultation
          </div>
          <nav className="space-y-0.5">
            {menuItems.map((item) => {
              const Icon = item.icon;
              const isActive = currentSection === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => setCurrentSection(item.id as NavigationSection)}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-xs font-medium transition-colors ${
                    isActive
                      ? 'bg-blue-600 text-white font-semibold shadow-sm'
                      : isDarkMode
                      ? 'hover:bg-slate-800 text-slate-300 hover:text-white'
                      : 'hover:bg-slate-200 text-slate-700 hover:text-slate-900'
                  }`}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{item.label}</span>
                </button>
              );
            })}
          </nav>
        </div>

        {/* Inventory Management Section */}
        <div>
          <div className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 font-mono">
            Gestion des inventaires
          </div>
          <nav className="space-y-0.5">
            {inventoryItems.map((item) => {
              const Icon = item.icon;
              const isActive = currentSection === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => setCurrentSection(item.id as NavigationSection)}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-xs font-medium transition-colors ${
                    isActive
                      ? 'bg-blue-600 text-white font-semibold shadow-sm'
                      : isDarkMode
                      ? 'hover:bg-slate-800 text-slate-300 hover:text-white'
                      : 'hover:bg-slate-200 text-slate-700 hover:text-slate-900'
                  }`}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{item.label}</span>
                </button>
              );
            })}
          </nav>
        </div>

        {/* System & Exit Section */}
        <div>
          <div className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 font-mono">
            Système
          </div>
          <nav className="space-y-0.5">
            {systemItems.map((item) => {
              const Icon = item.icon;
              const isActive = currentSection === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => setCurrentSection(item.id as NavigationSection)}
                  className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-xs font-medium transition-colors ${
                    isActive
                      ? 'bg-blue-600 text-white font-semibold shadow-sm'
                      : isDarkMode
                      ? 'hover:bg-slate-800 text-slate-300 hover:text-white'
                      : 'hover:bg-slate-200 text-slate-700 hover:text-slate-900'
                  }`}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{item.label}</span>
                </button>
              );
            })}
          </nav>
        </div>
      </div>
    </aside>
  );
};
