import React from 'react';

interface StatusBarProps {
  isDarkMode: boolean;
  lastInventoryDate: string;
}

export const StatusBar: React.FC<StatusBarProps> = ({ isDarkMode, lastInventoryDate }) => {
  return (
    <footer className={`px-4 py-1.5 text-xs flex items-center justify-between border-t select-none ${isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-400' : 'bg-slate-200 border-slate-300 text-slate-600'}`}>
      <div className="flex items-center gap-2">
        <span className="w-2 h-2 rounded-full bg-amber-500"></span>
        <span className="font-medium">Mode démonstration</span>
      </div>

      <div className="flex items-center gap-6 font-mono text-[11px]">
        <span>ATLAS PC v1.0.0</span>
        <span>Dernière mise à jour : {lastInventoryDate}</span>
      </div>
    </footer>
  );
};
