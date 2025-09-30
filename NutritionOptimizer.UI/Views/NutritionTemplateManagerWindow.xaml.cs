using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NutritionOptimizer.Domain;
using NutritionOptimizer.UI.ViewModels;

namespace NutritionOptimizer.UI.Views;

public partial class NutritionTemplateManagerWindow : Window
{
    private readonly INutritionTemplateRepository _repository;
    public ObservableCollection<NutritionTemplate> Templates { get; } = new();

    public NutritionTemplateManagerWindow(INutritionTemplateRepository repository)
    {
        InitializeComponent();
        _repository = repository;
        TemplatesDataGrid.ItemsSource = Templates;
        LoadTemplates();
    }

    // 템플릿 목록 불러오기
    private async void LoadTemplates()
    {
        try
        {
            var templates = await _repository.GetAllAsync();
            Templates.Clear();
            foreach (var template in templates.OrderBy(t => t.Name))
            {
                Templates.Add(template);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"템플릿 로드 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 추가 버튼
    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NutritionTemplateEditorWindow();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var newTemplate = dialog.ViewModel.ToTemplate();
                await _repository.AddAsync(newTemplate);
                LoadTemplates();
                MessageBox.Show("템플릿이 추가되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 추가 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 수정 버튼
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedTemplate = TemplatesDataGrid.SelectedItem as NutritionTemplate;
        if (selectedTemplate == null)
        {
            MessageBox.Show("수정할 템플릿을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var viewModel = new NutritionTemplateEditorViewModel(selectedTemplate);
        var dialog = new NutritionTemplateEditorWindow(viewModel);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var updatedTemplate = dialog.ViewModel.ToTemplate();
                await _repository.UpdateAsync(updatedTemplate);
                LoadTemplates();
                MessageBox.Show("템플릿이 수정되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 수정 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 삭제 버튼
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedTemplate = TemplatesDataGrid.SelectedItem as NutritionTemplate;
        if (selectedTemplate == null)
        {
            MessageBox.Show("삭제할 템플릿을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"'{selectedTemplate.Name}' 템플릿을 삭제하시겠습니까?",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _repository.DeleteAsync(selectedTemplate.Id);
                LoadTemplates();
                MessageBox.Show("템플릿이 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"템플릿 삭제 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 더블클릭으로 수정
    private void TemplatesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditButton_Click(sender, e);
    }

    // 닫기 버튼
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
