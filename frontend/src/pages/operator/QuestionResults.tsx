import React, { useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';

type AnswerResult = { answerId: string; text: string; count: number; percentage: number };

export default function QuestionResults({ quizId }: { quizId: string }) {
  const [results, setResults] = useState<AnswerResult[]>([]);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hub/game')
      .withAutomaticReconnect()
      .build();

    conn.on('QuestionResultsUpdated', (data: any) => {
      setResults(data as AnswerResult[]);
    });

    conn.start().catch(console.error);
    return () => { conn.stop(); };
  }, [quizId]);

  return (
    <div className="p-4">
      <h2 className="text-xl font-bold mb-2">Question Results</h2>
      <div className="space-y-2">
        {results.map(r => (
          <div key={r.answerId} className="w-full">
            <div className="flex justify-between mb-1">
              <span className="font-medium">{r.text}</span>
              <span className="text-sm text-gray-600">{r.count} votes ({r.percentage}%)</span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-4">
              <div className="bg-blue-500 h-4 rounded-full transition-all" style={{ width: `${r.percentage}%` }} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
