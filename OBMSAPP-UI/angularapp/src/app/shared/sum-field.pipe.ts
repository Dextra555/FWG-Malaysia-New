import { Pipe, PipeTransform } from '@angular/core';

/**
 * Sums a named numeric field across an array of objects.
 * Usage: {{ myArray | sumField:'fieldName' }}
 */
@Pipe({ name: 'sumField' })
export class SumFieldPipe implements PipeTransform {
  transform(items: any[], field: string): number {
    if (!Array.isArray(items) || !field) return 0;
    return items.reduce((acc, item) => acc + (+(item[field] ?? 0) || 0), 0);
  }
}
