import React, { useState } from 'react';
import { InstalledSoftware } from '../../types';
import { Search, Layers, Download, Trash2, Filter } from 'lucide-react';

interface SoftwareViewProps {
  softwareList: InstalledSoftware[];
  isDarkMode: boolean;
}

export const SoftwareView: React.FC<SoftwareViewProps> = ({ softwareList, isDarkMode }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('Tous');

  const categories = ['Tous', ...Array.from(new Set(softwareList.map(s => s.category)))];

  const filteredSoftware = softwareList.filter(s => {
    const matchesSearch = s.name.toLowerCase().includes(searchTerm.toLowerCase()) || s.publisher.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCat = selectedCategory === 'Tous' || s.category === selectedCategory;
    return matchesSearch && matchesCat;
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold">Logiciels installés</h2>
          <p className="text-xs opacity-75 mt-0.5">Liste des applications et logiciels détectés sur votre système ({softwareList.length} total).</p>
        </div>

        <div className="flex items-center gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 opacity-50" />
            <input
              type="text"
              placeholder="Rechercher un logiciel..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className={`pl-9 pr-4 py-2 rounded-xl text-xs border focus:outline-none focus:border-blue-500 w-64 ${
                isDarkMode ? 'bg-slate-900 border-slate-800 text-slate-100' : 'bg-white border-slate-300'
              }`}
            />
          </div>

          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className={`px-3 py-2 rounded-xl text-xs border focus:outline-none focus:border-blue-500 ${
              isDarkMode ? 'bg-slate-900 border-slate-800 text-slate-100' : 'bg-white border-slate-300'
            }`}
          >
            {categories.map(cat => (
              <option key={cat} value={cat}>{cat}</option>
            ))}
          </select>
        </div>
      </div>

      <div className={`rounded-xl border overflow-hidden ${isDarkMode ? 'bg-slate-900/80 border-slate-800' : 'bg-white border-slate-300 shadow-sm'}`}>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className={`border-b font-mono uppercase text-[10px] ${isDarkMode ? 'bg-slate-950 border-slate-800 text-slate-400' : 'bg-slate-100 border-slate-300 text-slate-600'}`}>
              <tr>
                <th className="px-4 py-3">Nom</th>
                <th className="px-4 py-3">Éditeur</th>
                <th className="px-4 py-3">Version</th>
                <th className="px-4 py-3">Catégorie</th>
                <th className="px-4 py-3">Taille</th>
                <th className="px-4 py-3">Installé le</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className={`divide-y ${isDarkMode ? 'divide-slate-800' : 'divide-slate-200'}`}>
              {filteredSoftware.map(sw => (
                <tr key={sw.id} className="hover:bg-blue-500/5 transition-colors">
                  <td className="px-4 py-3 font-bold text-blue-500">{sw.name}</td>
                  <td className="px-4 py-3 opacity-80">{sw.publisher}</td>
                  <td className="px-4 py-3 font-mono">{sw.version}</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-0.5 rounded text-[10px] bg-blue-500/10 text-blue-500 border border-blue-500/20 font-medium">
                      {sw.category}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-mono">{sw.size}</td>
                  <td className="px-4 py-3 font-mono">{sw.installDate}</td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => alert(`Détails de ${sw.name} version ${sw.version}`)}
                      className="px-2.5 py-1 rounded bg-blue-600/10 hover:bg-blue-600/20 text-blue-500 font-medium"
                    >
                      Détails
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
