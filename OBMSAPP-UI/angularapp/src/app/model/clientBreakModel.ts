export class ClientBreakModel {
    ID: number;
    ClientCode: string;
    BreakHours: number;
    SortOrder: number;
    CreatedDate: Date | null;

    constructor(data: Partial<ClientBreakModel> = {}) {
        this.ID = data.ID || 0;
        this.ClientCode = data.ClientCode || '';
        this.BreakHours = data.BreakHours ?? 0.5;
        this.SortOrder = data.SortOrder || 0;
        this.CreatedDate = data.CreatedDate || null;
    }
}
