using ProjectCook;
using Xunit;

public class PushScheduleTests
{
    // A build function that counts its calls and gives the JSON that the test sets.
    private sealed class Data
    {
        public string Json = "{\"a\":1}";
        public int Calls;
        public string Build() { Calls++; return Json; }
    }

    [Fact]
    public void No_request_sends_nothing_and_builds_nothing()
    {
        var s = new PushSchedule();
        var d = new Data();

        Assert.Equal(PushSchedule.Kind.None, s.Tick(10f, d.Build).Kind);
        Assert.Equal(0, d.Calls);
    }

    [Fact]
    public void Three_refreshes_in_one_frame_make_one_send()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.Request(10f);
        s.Request(10f);
        s.Request(10f);

        Assert.Equal(PushSchedule.Kind.SetData, s.Tick(10f, d.Build).Kind);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.02f, d.Build).Kind);
    }

    // Sends the data once and gives the page answer, then makes the next refresh.
    private static PushSchedule.Step SendAndAnswer(PushSchedule s, Data d, float now, string answer)
    {
        s.Request(now);
        var step = s.Tick(now, d.Build);
        s.OnResult(step, answer, now + 0.05f);
        return step;
    }

    [Fact]
    public void The_same_data_as_the_confirmed_data_goes_as_Apply_and_new_data_as_SetData()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, "installed");

        s.Request(20f);
        var same = s.Tick(20f, d.Build);
        Assert.Equal(PushSchedule.Kind.Apply, same.Kind);
        Assert.Equal("{\"a\":1}", same.Json);
        s.OnResult(same, "already installed", 20.05f);

        d.Json = "{\"a\":2}";
        s.Request(30f);
        var changed = s.Tick(30f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetData, changed.Kind);
        Assert.Equal("{\"a\":2}", changed.Json);
    }

    // setData stores the data before its pass, also when no Cooking frame is there or the pass fails (a
    // rebuilt browser blocks the frame), so each answer confirms the data, except an answer that shows that the
    // data did not arrive.
    [Theory]
    [InlineData("installed", PushSchedule.Kind.Apply)]
    [InlineData("already installed", PushSchedule.Kind.Apply)]
    [InlineData("no Cooking frame", PushSchedule.Kind.Apply)]
    [InlineData("installed; missing: tierMark(pot.getConfig)", PushSchedule.Kind.Apply)]
    [InlineData("already installed; errors: preview: x", PushSchedule.Kind.Apply)]
    [InlineData("error: SecurityError: Blocked a frame", PushSchedule.Kind.Apply)]
    [InlineData("no script", PushSchedule.Kind.SetData)]
    [InlineData(null, PushSchedule.Kind.SetData)]
    public void The_answer_of_a_SetData_send_decides_if_the_data_is_confirmed(string answer, PushSchedule.Kind next)
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, answer);

        s.Request(20f);
        Assert.Equal(next, s.Tick(20f, d.Build).Kind);
    }

    [Fact]
    public void After_no_script_the_next_send_carries_the_data()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, "installed");
        SendAndAnswer(s, d, 20f, "no script");

        s.Request(30f);
        Assert.Equal(PushSchedule.Kind.SetData, s.Tick(30f, d.Build).Kind);
    }

    [Fact]
    public void No_Cooking_frame_gives_a_retry_one_real_second_later_as_Apply_after_confirmed_data()
    {
        var s = new PushSchedule();
        var d = new Data();
        var first = SendAndAnswer(s, d, 10f, "no Cooking frame");
        Assert.False(first.Retry);

        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.5f, d.Build).Kind);
        var retry = s.Tick(11.05f, d.Build);
        Assert.Equal(PushSchedule.Kind.Apply, retry.Kind);
        Assert.True(retry.Retry);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(11.1f, d.Build).Kind);
    }

    [Fact]
    public void A_missing_result_counts_as_no_Cooking_frame()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, null);

        var retry = s.Tick(11.05f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetData, retry.Kind);
        Assert.True(retry.Retry);
    }

    [Fact]
    public void The_retries_stop_once_the_request_is_3_real_seconds_old()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, "no Cooking frame");

        var retry1 = s.Tick(11.05f, d.Build);
        s.OnResult(retry1, "no Cooking frame", 11.1f);
        var retry2 = s.Tick(12.1f, d.Build);
        Assert.True(retry2.Retry);
        s.OnResult(retry2, "no Cooking frame", 12.15f);

        Assert.Equal(PushSchedule.Kind.None, s.Tick(13.15f, d.Build).Kind);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(20f, d.Build).Kind);
    }

    [Fact]
    public void A_refresh_during_a_retry_wait_sends_at_once_and_starts_the_3_seconds_again()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, "no Cooking frame");

        s.Request(12.9f);
        var step = s.Tick(12.9f, d.Build);
        Assert.Equal(PushSchedule.Kind.Apply, step.Kind);
        Assert.False(step.Retry);
        s.OnResult(step, "no Cooking frame", 12.95f);

        Assert.True(s.Tick(13.95f, d.Build).Retry);
    }

    [Fact]
    public void A_pass_that_finds_only_a_storage_window_gives_no_retry()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, PageJson.StorageResultPrefix + "installed");

        Assert.Equal(PushSchedule.Kind.None, s.Tick(11.05f, d.Build).Kind);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(12.1f, d.Build).Kind);
    }

    [Fact]
    public void A_storage_window_answer_with_a_missing_part_gives_no_retry()
    {
        var s = new PushSchedule();
        var d = new Data();
        SendAndAnswer(s, d, 10f, PageJson.StorageResultPrefix + "installed; missing: tagIcon(#app)");

        Assert.Equal(PushSchedule.Kind.None, s.Tick(11.05f, d.Build).Kind);
    }

    private const string Sort = "{\"owner\":\"1\"}";
    private const string Sort2 = "{\"owner\":\"2\"}";

    [Fact]
    public void Sort_data_alone_goes_as_SetSortData_with_the_sort_data()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);

        var step = s.Tick(10f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, step.Kind);
        Assert.Equal(Sort, step.SortJson);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.02f, d.Build).Kind);
    }

    [Fact]
    public void The_same_sort_data_as_the_confirmed_one_gives_no_send()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);
        var step = s.Tick(10f, d.Build);
        s.OnResult(step, PageJson.StorageResultPrefix + "installed", 10.05f);

        s.RequestSort(Sort);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(11f, d.Build).Kind);
        s.RequestSort(Sort2);
        Assert.Equal(PushSchedule.Kind.SetSortData, s.Tick(12f, d.Build).Kind);
    }

    [Fact]
    public void A_window_change_with_a_refresh_and_sort_data_makes_one_send()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.Request(10f);
        s.RequestSort(Sort);

        var step = s.Tick(10f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetData, step.Kind);
        Assert.Equal(Sort, step.SortJson);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.02f, d.Build).Kind);
    }

    [Fact]
    public void A_send_without_new_sort_data_carries_none()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.Request(10f);
        Assert.Null(s.Tick(10f, d.Build).SortJson);
    }

    [Fact]
    public void No_script_makes_the_next_send_carry_the_sort_data_again()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);
        var first = s.Tick(10f, d.Build);
        s.OnResult(first, PageJson.StorageResultPrefix + "installed", 10.05f);

        s.Request(20f);
        var second = s.Tick(20f, d.Build);
        s.OnResult(second, PageJson.NoScript, 20.05f);

        s.RequestSort(Sort);
        var third = s.Tick(21f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, third.Kind);
        Assert.Equal(Sort, third.SortJson);
    }

    [Fact]
    public void A_sort_send_with_no_answer_is_sent_again()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);
        var first = s.Tick(10f, d.Build);
        s.OnResult(first, null, 10.05f);

        var again = s.Tick(11.1f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, again.Kind);
        Assert.Equal(Sort, again.SortJson);
    }

    [Fact]
    public void A_sort_send_whose_answer_never_comes_is_sent_again_after_the_wait()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);
        var first = s.Tick(10f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, first.Kind);

        // No OnResult: a browser crash dropped the callback.
        s.RequestSort(Sort);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(11f, d.Build).Kind);
        var again = s.Tick(15.1f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, again.Kind);
        Assert.Equal(Sort, again.SortJson);

        // The late answer of the first send does not free the second one.
        s.OnResult(first, null, 15.2f);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(16.5f, d.Build).Kind);
    }

    [Fact]
    public void A_sort_only_build_that_throws_waits_one_real_second()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.RequestSort(Sort);
        Assert.Throws<InvalidOperationException>(() => s.Tick(10f, () => throw new InvalidOperationException()));

        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.02f, d.Build).Kind);
        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.9f, d.Build).Kind);
        var again = s.Tick(11.05f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetSortData, again.Kind);
        Assert.Equal(Sort, again.SortJson);
    }

    [Fact]
    public void A_build_that_throws_is_tried_again_one_real_second_later()
    {
        var s = new PushSchedule();
        var d = new Data();
        s.Request(10f);
        Assert.Throws<InvalidOperationException>(() => s.Tick(10f, () => throw new InvalidOperationException()));

        Assert.Equal(PushSchedule.Kind.None, s.Tick(10.5f, d.Build).Kind);
        var retry = s.Tick(11f, d.Build);
        Assert.Equal(PushSchedule.Kind.SetData, retry.Kind);
        Assert.True(retry.Retry);
    }
}
