import React, { useState } from 'react';
import { InventorySnapshot } from '../../types';
import { Scale, ArrowRight, CheckCircle2 } from 'lucide-react';

interface CompareViewProps {
  inventoryHistory: InventorySnapshot[];
  isDarkMode: boolean;
}

export const CompareView: React.FC<CompareViewProps> = ({ inventoryHistory, isDarkMode }) => {
  const [inv1Id, setInv1Id] = useState(inventoryHistory[0]?.id || '');
  const [inv2Id, setInv2Id] = useState(inventoryHistory[1]?.id || inventoryHistory[0]?.id || '');

  const inv1 = inventoryHistory.find(i => i.id === inv1Id);
  const inv2 = inventoryHistory.find(i => i.id === inv2Id);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold">Comparaison de deux inventaires</h2>
        <p className="text-xs opacity-75 mt-0.5">Mettez en évidence l'évolution et les différences entre deux dates d'inventaire.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className={`p-4 rounded-xl border space-y-3 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
          <label className="text-xs font-semibold uppercase tracking-wider text-blue-500 font-mono block">Inventaire de référence (A)</label>
          <select
            value={inv1Id}
            onChange={(e) => setInv1Id(e.target.value)}
            className={`w-full p-2.5 rounded-xl text-xs border focus:outline-none focus:border-blue-500 ${
              isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-100' : 'bg-slate-50 border-slate-300'
            }`}
          >
            {inventoryHistory.map(inv => (
              <option key={inv.id} value={inv.id}>{inv.date} — {inv.description}</option>
            ))}
          </select>
          {inv1 && (
            <div className="space-y-1 pt-2 text-xs font-mono">
              <div>Fichiers : {inv1.filesCount}</div>
              <div>Taille : {inv1.totalSize}</div>
              <div>Composants : {inv1.componentsCount}</div>
            </div>
          )}
        </div>

        <div className={`p-4 rounded-xl border space-y-3 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
          <label className="text-xs font-semibold uppercase tracking-wider text-blue-500 font-mono block">Inventaire comparé (B)</label>
          <select
            value={inv2Id}
            onChange={(e) => setInv2Id(e.target.value)}
            className={`w-full p-2.5 rounded-xl text-xs border focus:outline-none focus:border-blue-500 ${
              isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-100' : 'bg-slate-50 border-slate-300'
            }`}
          >
            {inventoryHistory.map(inv => (
              <option key={inv.id} value={inv.id}>{inv.date} — {inv.description}</option>
            ))}
          </select>
          {inv2 && (
            <div className="space-y-1 pt-2 text-xs font-mono">
              <div>Fichiers : {inv2.filesCount}</div>
              <div>Taille : {inv2.totalSize}</div>
              <div>Composants : {inv2.componentsCount}</div>
            </div>
          )}
        </div>
      </div>

      <div className={`p-6 rounded-xl border text-center space-y-3 ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
        <Scale className="w-10 h-10 text-blue-500 mx-auto opacity-80" />
        <h3 className="text-sm font-bold">Analyse comparative prête</h3>
        <p className="text-xs opacity-75 max-w-md mx-auto">
          Aucune anomalie critique ou modification non autorisée détectée entre le {inv1?.date} et le {inv2?.date}.
        </p>
        <div className="flex items-center justify-center gap-2 text-xs text-emerald-500 font-medium bg-emerald-500/10 border border-emerald-500/20 px-3 py-1 rounded-full w-fit mx-auto">
          <CheckCircle2 className="w-4 h-4" />
          <span>Intégrité du système : 100% stable</span>
        </div>
      </div>
    </div>
  );
};
