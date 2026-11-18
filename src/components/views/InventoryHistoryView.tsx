import React from 'react';
import { InventorySnapshot } from '../../types';
import { History, Calendar, Plus, Trash2, Eye, RotateCcw } from 'lucide-react';

interface InventoryHistoryViewProps {
  inventoryHistory: InventorySnapshot[];
  isDarkMode: boolean;
  onNewInventory: () => void;
  onDeleteInventory: (id: string) => void;
}

export const InventoryHistoryView: React.FC<InventoryHistoryViewProps> = ({
  inventoryHistory,
  isDarkMode,
  onNewInventory,
  onDeleteInventory,
}) => {
  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold">Gestion et historique des inventaires</h2>
          <p className="text-xs opacity-75 mt-0.5">Conservez, restaurez et comparez vos inventaires datés pour suivre l'évolution de votre PC.</p>
        </div>
        <button
          onClick={onNewInventory}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold shadow-md transition-all active:scale-95 shrink-0"
        >
          <Plus className="w-4 h-4" />
          <span>Créer un nouvel inventaire</span>
        </button>
      </div>

      <div className={`rounded-xl border overflow-hidden ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className={`border-b font-mono uppercase text-[10px] ${isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-400' : 'bg-slate-100 border-slate-300 text-slate-600'}`}>
              <tr>
                <th className="px-4 py-3">Date et heure</th>
                <th className="px-4 py-3">Description</th>
                <th className="px-4 py-3">Version d'ATLAS</th>
                <th className="px-4 py-3">Éléments</th>
                <th className="px-4 py-3">Taille totale</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className={`divide-y ${isDarkMode ? 'divide-slate-800' : 'divide-slate-200'}`}>
              {inventoryHistory.map(inv => (
                <tr key={inv.id} className="hover:bg-blue-500/5 transition-colors">
                  <td className="px-4 py-3 font-mono font-medium">{inv.date}</td>
                  <td className="px-4 py-3 font-bold">{inv.description}</td>
                  <td className="px-4 py-3 font-mono">{inv.version}</td>
                  <td className="px-4 py-3 font-mono">{inv.componentsCount} composants / {inv.filesCount} fichiers</td>
                  <td className="px-4 py-3 font-mono">{inv.totalSize}</td>
                  <td className="px-4 py-3 text-right flex items-center justify-end gap-2">
                    <button
                      onClick={() => alert(`Consultation de l'inventaire du ${inv.date}`)}
                      className="p-1.5 rounded bg-blue-600/10 hover:bg-blue-600/20 text-blue-500"
                      title="Voir"
                    >
                      <Eye className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => alert(`Restauration de l'inventaire du ${inv.date}`)}
                      className="p-1.5 rounded bg-emerald-600/10 hover:bg-emerald-600/20 text-emerald-500"
                      title="Restaurer"
                    >
                      <RotateCcw className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => onDeleteInventory(inv.id)}
                      className="p-1.5 rounded bg-rose-600/10 hover:bg-rose-600/20 text-rose-500"
                      title="Supprimer"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
