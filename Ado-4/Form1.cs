using System.Configuration;
using System.Data.SqlClient;
using System.Text;

namespace Ado4;

public partial class Form1 : Form
{
    SqlConnection? conn = null;
    int authorsId;
    bool isConnectionStringOpen = false;
    public Form1()
    {
        InitializeComponent();
        ////Configuration String connection add method
        //ConnectionStringAdd("Key", "Data Source=R0Y4L;Initial Catalog=Library;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        conn = new SqlConnection(ConfigurationManager.AppSettings.Get("Key"));
        AuthorsReader(comboBox1);
    }

    #region Methods
    private static void ConnectionStringAdd(string key, string value)
    {
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Add(key, value);
        config.Save(ConfigurationSaveMode.Modified);
    }
    private void AuthorsReader(ComboBox cbox)
    {
        SqlDataReader? reader = null;

        if (!isConnectionStringOpen)
        {
            isConnectionStringOpen = true;
            conn?.Open();
        }

        using SqlCommand cmdAuthors = new SqlCommand("SELECT * FROM Authors WAITFOR DELAY '00:00:03'", conn);

        cmdAuthors.BeginExecuteReader(ar =>
        {
            try
            {
                bool flag = false;
                using (reader = cmdAuthors.EndExecuteReader(ar))
                {
                    while (reader.Read())
                    {
                        if (flag)
                        {
                            StringBuilder field = new StringBuilder();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (i == 0)
                                    field.Append(reader[i].ToString() + '.');
                                else
                                    field.Append(reader[i].ToString() + ' ');
                            }

                            if (cbox.InvokeRequired)
                            {
                                cbox.Invoke(() =>
                                {
                                    cbox.Items.Add(field.ToString());
                                });
                            }
                        }
                        else
                            flag = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
                conn?.Close();
                isConnectionStringOpen = false;
            }
        }, null);
    }

    public void CategoriesReader(ComboBox cbox)
    {
        comboBox2.Items.Clear();

        SqlDataReader? reader = null;

        string textCmd = @"SELECT DISTINCT Categories.Name FROM Books
JOIN Authors
ON Books.Id_Author = Authors.Id
JOIN Categories
ON Categories.Id = Books.Id_Category
WHERE Authors.Id = @id
WaitFor Delay '00:00:03'";

        if (!isConnectionStringOpen)
        {
            isConnectionStringOpen = true;
            conn?.Open();
        }

        using SqlCommand cmdCategories = conn?.CreateCommand()!;
        cmdCategories!.CommandText = textCmd;

        StringBuilder? value = new();
        if(comboBox1.SelectedItem != null)
        {
            for (int i = 0; i < comboBox1.SelectedItem.ToString()?.Length; i++)
            {
                if (comboBox1.SelectedItem.ToString()?[i].ToString() != ".")
                    value.Append(comboBox1.SelectedItem.ToString()?[i].ToString());
                else
                    break;
            }
        }

        authorsId = int.Parse(value.ToString());
        cmdCategories.Parameters.AddWithValue("@id", authorsId);

        cmdCategories.BeginExecuteReader(result =>
        {
            try
            {
                using (reader = cmdCategories.EndExecuteReader(result))
                {
                    while (reader.Read())
                        for (int i = 0; i < reader.FieldCount; i++)
                            if (cbox.InvokeRequired)
                                cbox.Invoke(() =>
                                {
                                    cbox.Items.Add(reader[i].ToString());
                                });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                reader?.Close();
                conn?.Close();
                isConnectionStringOpen = false;
            }
        }, null);
    }
    private void BooksReader(ListView lView)
    {
        SqlDataReader? reader = null;

        if (!isConnectionStringOpen)
        {
            isConnectionStringOpen = true;
            conn?.Open();
        }

        using SqlCommand cmdCategories = new SqlCommand(@"SELECT Books.Id, Books.Name AS [Books], Authors.FirstName + ' ' + Authors.LastName AS [FullName of Authors], Categories.Name AS [Categories] FROM Books
JOIN Authors
ON Books.Id_Author = Authors.Id
JOIN Categories
ON Categories.Id = Books.Id_Category
WHERE Categories.Name = @categoryName AND Authors.Id = @authorsId
WAITFOR DELAY '00:00:02'", conn);

        StringBuilder? value = new();

        lView.Items.Clear();
        lView.Columns.Clear();

        cmdCategories.Parameters.AddWithValue("@categoryName", comboBox2.SelectedItem.ToString());
        cmdCategories.Parameters.AddWithValue("@authorsId", authorsId);

        lView.View = View.Details;
        lView.Columns.Add("Number");
        lView.Columns.Add("Books Id");
        lView.Columns.Add("Books Name");
        lView.Columns.Add("FullName of Authors");
        lView.Columns.Add("Category");

        cmdCategories.BeginExecuteReader(result =>
        {
            try
            {
                using (reader = cmdCategories.EndExecuteReader(result))
                {
                    int line = 0;
                    while (reader.Read())
                    {
                        ListViewItem item = new ListViewItem((line + 1).ToString());

                        for (int i = 0; i < reader.FieldCount; i++)
                            item.SubItems.Add(reader[i].ToString());
                        
                        lView.Invoke(() =>
                        {
                            lView.Items.Add(item);
                        });
                        
                        ++line;
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                conn?.Close();
                reader?.Close();
                isConnectionStringOpen = false;
            }

        }, null);
    }

    private void BooksSearch(ListView lView, TextBox textBox)
    {
        SqlDataReader? reader = null;

        try
        {
            conn?.Open();

            using SqlCommand cmdCategories = new SqlCommand(@"SELECT Books.Id, Books.Name AS [Books], Authors.FirstName + ' ' + Authors.LastName AS [FullName of Authors], Categories.Name AS [Categories] FROM Books
JOIN Authors
ON Books.Id_Author = Authors.Id
JOIN Categories
ON Categories.Id = Books.Id_Category
WHERE Books.Name = @booksName OR Books.Id = @booksId", conn);

            StringBuilder? value = new();

            listView.View = View.Tile;
            lView.Items.Clear();
            lView.Columns.Clear();

            if (int.TryParse(textBox.Text, out int id))
            {
                cmdCategories.Parameters.AddWithValue("@booksId", id);
                cmdCategories.Parameters.AddWithValue("@booksName", "");
            }
            else
            {
                cmdCategories.Parameters.AddWithValue("@booksId", 0);
                cmdCategories.Parameters.AddWithValue("@booksName", textBox.Text);
            }
            reader = cmdCategories.ExecuteReader();

            lView.View = View.Details;
            lView.Columns.Add("Number");
            lView.Columns.Add("Books Id");
            lView.Columns.Add("Books Name");
            lView.Columns.Add("FullName of Authors");
            lView.Columns.Add("Category");

            int line = 0;
            while (reader.Read())
            {
                ListViewItem item = new ListViewItem((line + 1).ToString());

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    item.SubItems.Add(reader[i].ToString());
                }
                lView.Items.Add(item);
                ++line;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            conn?.Close();
            reader?.Close();
        }
    }

    #endregion

    #region Events
    private void comboBox_SelectedValueChanged(object sender, EventArgs e)
    {
        ComboBox? cbox = sender as ComboBox;

        if (cbox == comboBox1)
            CategoriesReader(comboBox2);
        else if (cbox == comboBox2)
            BooksReader(listView);
    }
    private void button1_Click(object sender, EventArgs e)
    {
        BooksSearch(listView, textBox1);
    }
    private void textBox1_Click(object sender, EventArgs e)
    {
        textBox1.Text = "";
    }
    #endregion
}