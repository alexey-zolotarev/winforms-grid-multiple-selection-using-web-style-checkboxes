' Multiple row selection using custom checkbox column (web style)
' 

Imports System
Imports System.Drawing
Imports System.Collections
Imports System.Windows.Forms
Imports DevExpress.XtraGrid.Columns
Imports DevExpress.XtraEditors.Controls
Imports DevExpress.XtraEditors.Repository
Imports DevExpress.XtraGrid.Views.Base
Imports DevExpress.XtraGrid.Views.Grid
Imports DevExpress.XtraGrid.Views.Grid.ViewInfo
Imports DevExpress.Utils.Drawing
Imports DevExpress.XtraEditors.ViewInfo
Imports DevExpress.XtraEditors.Drawing

Namespace DevExpress.XtraGrid.Selection

    Public Class GridCheckMarksSelection

        Protected _view As GridView

        Protected selection As ArrayList

        Private column As GridColumn

        Private edit As RepositoryItemCheckEdit

        Const CheckboxIndent As Integer = 4

        Public Sub New()
            selection = New ArrayList()
        End Sub

        Public Sub New(ByVal view As GridView)
            Me.New()
            Me.View = view
        End Sub

        Public Property View As GridView
            Get
                Return _view
            End Get

            Set(ByVal value As GridView)
                If _view IsNot value Then
                    Detach()
                    Attach(value)
                End If
            End Set
        End Property

        Public ReadOnly Property CheckMarkColumn As GridColumn
            Get
                Return column
            End Get
        End Property

        Public ReadOnly Property SelectedCount As Integer
            Get
                Return selection.Count
            End Get
        End Property

        Public Function GetSelectedRow(ByVal index As Integer) As Object
            Return selection(index)
        End Function

        Public Function GetSelectedIndex(ByVal row As Object) As Integer
            Return selection.IndexOf(row)
        End Function

        Public Sub ClearSelection()
            selection.Clear()
            Invalidate()
        End Sub

        Public Sub SelectAll()
            selection.Clear()
            ' fast (won't work if the grid is filtered)
            'if(_view.DataSource is ICollection)
            '	selection.AddRange(((ICollection)_view.DataSource));
            'else
            ' slow:
            For i As Integer = 0 To _view.DataRowCount - 1
                selection.Add(_view.GetRow(i))
            Next

            Invalidate()
        End Sub

        Public Sub SelectGroup(ByVal rowHandle As Integer, ByVal [select] As Boolean)
            If IsGroupRowSelected(rowHandle) AndAlso [select] Then Return
            For i As Integer = 0 To _view.GetChildRowCount(rowHandle) - 1
                Dim childRowHandle As Integer = _view.GetChildRowHandle(rowHandle, i)
                If _view.IsGroupRow(childRowHandle) Then
                    SelectGroup(childRowHandle, [select])
                Else
                    SelectRow(childRowHandle, [select], False)
                End If
            Next

            Invalidate()
        End Sub

        Public Sub SelectRow(ByVal rowHandle As Integer, ByVal [select] As Boolean)
            SelectRow(rowHandle, [select], True)
        End Sub

        Public Sub InvertRowSelection(ByVal rowHandle As Integer)
            If View.IsDataRow(rowHandle) Then
                SelectRow(rowHandle, Not IsRowSelected(rowHandle))
            End If

            If View.IsGroupRow(rowHandle) Then
                SelectGroup(rowHandle, Not IsGroupRowSelected(rowHandle))
            End If
        End Sub

        Public Function IsGroupRowSelected(ByVal rowHandle As Integer) As Boolean
            For i As Integer = 0 To _view.GetChildRowCount(rowHandle) - 1
                Dim row As Integer = _view.GetChildRowHandle(rowHandle, i)
                If _view.IsGroupRow(row) Then
                    If Not IsGroupRowSelected(row) Then Return False
                ElseIf Not IsRowSelected(row) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Public Function IsRowSelected(ByVal rowHandle As Integer) As Boolean
            If _view.IsGroupRow(rowHandle) Then Return IsGroupRowSelected(rowHandle)
            Dim row As Object = _view.GetRow(rowHandle)
            Return GetSelectedIndex(row) <> -1
        End Function

        Protected Overridable Sub Attach(ByVal view As GridView)
            If view Is Nothing Then Return
            selection.Clear()
            _view = view
            view.BeginUpdate()
            Try
                edit = TryCast(view.GridControl.RepositoryItems.Add("CheckEdit"), RepositoryItemCheckEdit)
                column = view.Columns.Add()
                column.OptionsColumn.AllowSort = Utils.DefaultBoolean.False
                column.Visible = True
                column.VisibleIndex = 0
                column.FieldName = "CheckMarkSelection"
                column.Caption = "Mark"
                column.OptionsColumn.ShowCaption = False
                column.OptionsColumn.AllowEdit = False
                column.OptionsColumn.AllowSize = False
                column.UnboundType = Data.UnboundColumnType.Boolean
                column.Width = GetCheckBoxWidth()
                column.ColumnEdit = edit
                AddHandler view.Click, New EventHandler(AddressOf View_Click)
                AddHandler view.CustomDrawColumnHeader, New ColumnHeaderCustomDrawEventHandler(AddressOf View_CustomDrawColumnHeader)
                AddHandler view.CustomDrawGroupRow, New RowObjectCustomDrawEventHandler(AddressOf View_CustomDrawGroupRow)
                AddHandler view.CustomUnboundColumnData, New CustomColumnDataEventHandler(AddressOf view_CustomUnboundColumnData)
                AddHandler view.KeyDown, New KeyEventHandler(AddressOf view_KeyDown)
                AddHandler view.RowStyle, New RowStyleEventHandler(AddressOf view_RowStyle)
            Finally
                view.EndUpdate()
            End Try
        End Sub

        Protected Overridable Sub Detach()
            If _view Is Nothing Then Return
            If column IsNot Nothing Then column.Dispose()
            If edit IsNot Nothing Then
                _view.GridControl.RepositoryItems.Remove(edit)
                edit.Dispose()
            End If

            RemoveHandler _view.Click, New EventHandler(AddressOf View_Click)
            RemoveHandler _view.CustomDrawColumnHeader, New ColumnHeaderCustomDrawEventHandler(AddressOf View_CustomDrawColumnHeader)
            RemoveHandler _view.CustomDrawGroupRow, New RowObjectCustomDrawEventHandler(AddressOf View_CustomDrawGroupRow)
            RemoveHandler _view.CustomUnboundColumnData, New CustomColumnDataEventHandler(AddressOf view_CustomUnboundColumnData)
            RemoveHandler _view.KeyDown, New KeyEventHandler(AddressOf view_KeyDown)
            RemoveHandler _view.RowStyle, New RowStyleEventHandler(AddressOf view_RowStyle)
            _view = Nothing
        End Sub

        Protected Function GetCheckBoxWidth() As Integer
            Dim info As CheckEditViewInfo = TryCast(edit.CreateViewInfo(), CheckEditViewInfo)
            Dim width As Integer = 0
            Call GraphicsInfo.Default.AddGraphics(Nothing)
            Try
                width = info.CalcBestFit(GraphicsInfo.Default.Graphics).Width
            Finally
                Call GraphicsInfo.Default.ReleaseGraphics()
            End Try

            Return width + CheckboxIndent * 2
        End Function

        Protected Sub DrawCheckBox(ByVal cache As GraphicsCache, ByVal r As Rectangle, ByVal Checked As Boolean)
            Dim info As CheckEditViewInfo
            Dim painter As CheckEditPainter
            Dim args As ControlGraphicsInfoArgs
            info = TryCast(edit.CreateViewInfo(), CheckEditViewInfo)
            painter = TryCast(edit.CreatePainter(), CheckEditPainter)
            info.EditValue = Checked
            info.Bounds = r
            info.CalcViewInfo(Nothing)
            args = New ControlGraphicsInfoArgs(info, cache, r)
            painter.Draw(args)
        End Sub

        Private Sub Invalidate()
            _view.CloseEditor()
            _view.BeginUpdate()
            _view.EndUpdate()
        End Sub

        Private Sub SelectRow(ByVal rowHandle As Integer, ByVal [select] As Boolean, ByVal invalidate As Boolean)
            If IsRowSelected(rowHandle) = [select] Then Return
            Dim row As Object = _view.GetRow(rowHandle)
            If [select] Then
                selection.Add(row)
            Else
                selection.Remove(row)
            End If

            If invalidate Then
                Me.Invalidate()
            End If
        End Sub

        Private Sub view_CustomUnboundColumnData(ByVal sender As Object, ByVal e As CustomColumnDataEventArgs)
            If e.Column Is CheckMarkColumn Then
                If e.IsGetData Then
                    e.Value = IsRowSelected(View.GetRowHandle(e.ListSourceRowIndex))
                Else
                    SelectRow(View.GetRowHandle(e.ListSourceRowIndex), CBool(e.Value))
                End If
            End If
        End Sub

        Private Sub view_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs)
            If View.FocusedColumn IsNot column OrElse e.KeyCode <> Keys.Space Then Return
            InvertRowSelection(View.FocusedRowHandle)
        End Sub

        Private Sub View_Click(ByVal sender As Object, ByVal e As EventArgs)
            Dim info As GridHitInfo
            Dim pt As Point = _view.GridControl.PointToClient(Control.MousePosition)
            info = _view.CalcHitInfo(pt)
            If info.Column Is column Then
                If info.InColumn Then
                    If SelectedCount = _view.DataRowCount Then
                        ClearSelection()
                    Else
                        SelectAll()
                    End If
                End If

                If info.InRowCell Then
                    InvertRowSelection(info.RowHandle)
                End If
            End If

            If info.InRow AndAlso _view.IsGroupRow(info.RowHandle) AndAlso info.HitTest <> GridHitTest.RowGroupButton Then
                InvertRowSelection(info.RowHandle)
            End If
        End Sub

        Private Sub View_CustomDrawColumnHeader(ByVal sender As Object, ByVal e As ColumnHeaderCustomDrawEventArgs)
            If e.Column Is column Then
                e.Info.InnerElements.Clear()
                e.Painter.DrawObject(e.Info)
                DrawCheckBox(e.Cache, e.Bounds, SelectedCount = _view.DataRowCount)
                e.Handled = True
            End If
        End Sub

        Private Sub View_CustomDrawGroupRow(ByVal sender As Object, ByVal e As RowObjectCustomDrawEventArgs)
            Dim info As GridGroupRowInfo = TryCast(e.Info, GridGroupRowInfo)
            info.GroupText = "         " & info.GroupText.TrimStart()
            e.Appearance.FillRectangle(e.Cache, e.Bounds)
            e.Painter.DrawObject(e.Info)
            Dim r As Rectangle = info.ButtonBounds
            r.Offset(r.Width + CheckboxIndent * 2 - 1, 0)
            DrawCheckBox(e.Cache, r, IsGroupRowSelected(e.RowHandle))
            e.Handled = True
        End Sub

        Private Sub view_RowStyle(ByVal sender As Object, ByVal e As RowStyleEventArgs)
            If IsRowSelected(e.RowHandle) Then
                e.Appearance.BackColor = SystemColors.Highlight
                e.Appearance.ForeColor = SystemColors.HighlightText
            End If
        End Sub
    End Class
End Namespace
